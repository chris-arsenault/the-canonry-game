RESTORE_FLAGS ?= /p:NuGetAudit=false
SHELL := /bin/bash
.SHELLFLAGS := -o pipefail -c

# Tests tagged [Trait("Category", "Slow")] build full worlds, kernels, domain
# configurations, or Monte Carlo batches. They dominate wall time, so the
# default loop skips them; run `make ci-full` before pushing anything that
# touches worldgen, startup, wiring, or the generator catalog.
SLOW_FILTER ?= Category=Slow
FAST_FILTER ?= Category!=Slow
TEST_LOG_DIR ?= .artifacts/test-logs

define run-tests
	@mkdir -p "$(TEST_LOG_DIR)"
	@log="$(TEST_LOG_DIR)/$(2).log"; \
	echo "Writing full test output to $$log"; \
	dotnet test -c Release --verbosity normal \
		--settings test.runsettings --maxcpucount:1 $(1) >"$$log" 2>&1; \
	status=$$?; \
	if (( status != 0 )); then \
		echo; \
		echo "Test command failed; full log: $$log"; \
		echo "Failure summary:"; \
		grep -n -E -B 4 -A 10 '(^|  )(Failed |Error Message:|Test Run Failed|Build FAILED|MSB[0-9]+)' "$$log" \
			| tail -n 240 || true; \
	else \
		grep -E '^(Passed!|Test Run Successful\\.|Total tests:|[[:space:]]+Passed:)' "$$log" \
			| tail -n 80 || true; \
	fi; \
	exit "$$status"
endef

.PHONY: ci ci-full build test test-slow test-all lint analyzer-lint format-check clean domain-check mutation

ci: lint test domain-check

ci-full: lint test-all domain-check

build:
	dotnet restore $(RESTORE_FLAGS)
	dotnet build -c Release --no-restore

test:
	$(call run-tests,--filter "$(FAST_FILTER)",test)

test-slow:
	$(call run-tests,--filter "$(SLOW_FILTER)",test-slow)

test-all:
	$(call run-tests,,test-all)

lint: analyzer-lint format-check

analyzer-lint:
	dotnet restore $(RESTORE_FLAGS)
	dotnet build TheCanonryGame.slnx -c Release --no-restore -warnaserror $(RESTORE_FLAGS) /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true /p:EnforceCodeStyleInBuild=true

format-check:
	dotnet format whitespace TheCanonryGame.slnx --verify-no-changes --no-restore --verbosity minimal
	dotnet format style TheCanonryGame.slnx --verify-no-changes --severity warn --no-restore --verbosity minimal

domain-check:
	@echo "Checking engine for domain contamination..."
	@if grep -ri 'penguin\|orca\|soup\|ice magic\|long dark\|frosthollow\|iceberg' src/Engine/ 2>/dev/null; then \
		echo "FAIL: Domain-specific terms found in engine code"; exit 1; \
	else \
		echo "PASS: No domain contamination"; \
	fi

# Scoped mutation testing over the generator's algorithmic core. On demand or
# nightly; see tools/mutation/README.md. Not part of ci.
mutation:
	cd tools/mutation && dotnet stryker --config-file stryker-config.json

# --- Simulation runs, worldgen, and scoped tests -----------------------------
# These are the canonical invocations. Do not hand-roll the underlying dotnet
# commands (see AGENTS.md): they encode the flags that repeatedly get wrong -
# Release configuration (Debug turns minutes into hours), unique MC run ids
# (the registry rejects reuse), fresh builds (MC refuses stale binaries), and
# the tools' current argument names.

MC_RUNS ?= 6
MC_TICKS ?= 200
MC_SEED ?= 1000
MC_WORKERS ?= 1
MC_EXTRA ?=
RUN_ID ?= dev-$(shell date +%Y%m%d-%H%M%S)
MC_STATS ?=
SEED ?= 42
DIAG_FLAGS ?= --hex-contract
PROFILE_RUN_ID ?= preparallel-profile-$(shell date +%Y%m%d-%H%M%S)
PROFILE_TICKS ?= 1000
PROFILE_SEED ?= 4200
PROFILE_WORLD ?= worlds/ice-remembers-seed-$(PROFILE_SEED).world
PROFILE_BASE ?= .artifacts/profiles/$(PROFILE_RUN_ID)
DOTNET_TRACE ?= $(HOME)/.dotnet/tools/dotnet-trace
MONTE_CARLO_DLL ?= .artifacts/bin/MonteCarlo/Release/net8.0/MonteCarlo.dll
MC_SMOKE_TICKS ?= 1000
MC_SMOKE_SEED ?= 4200
MC_SMOKE_WORLD ?= worlds/ice-remembers-seed-$(MC_SMOKE_SEED).world

.PHONY: mc mc-smoke mc-show mc-append-stats mc-delete profile-kernel world capture diagnose test-kernel test-domain test-eventing test-seed test-gameplay

# Monte Carlo batch. Sizing guide: quick "did the number move" check =
# MC_RUNS=2; calibration report = MC_RUNS=6+ at the horizon you are judging.
# Example: make mc MC_RUNS=2 MC_TICKS=5000 RUN_ID=my-question
mc:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		--runs $(MC_RUNS) --ticks $(MC_TICKS) --seed $(MC_SEED) \
		--workers $(MC_WORKERS) \
		--run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo $(MC_EXTRA)

# Fast one-world feedback path. The versioned Release-generated world must
# already exist; use `make world SEED=$(MC_SMOKE_SEED)` to replace it explicitly.
mc-smoke:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		--runs 1 --ticks $(MC_SMOKE_TICKS) --seed $(MC_SMOKE_SEED) \
		--workers $(MC_WORKERS) \
		--world $(MC_SMOKE_WORLD) \
		--run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo \
		--simulation-only --no-analysis

mc-show:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		show --run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo

mc-append-stats:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		append-stats --run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo \
		$(MC_STATS)

mc-delete:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		delete-run --run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo

# Bounded one-seed simulation-only CPU profile for scheduler/phase work.
# The registry run and trace share PROFILE_RUN_ID so the evidence remains
# attributable without adding profiler concerns to the kernel.
profile-kernel:
	mkdir -p .artifacts/profiles
	dotnet build tools/MonteCarlo/MonteCarlo.csproj -c Release
	$(DOTNET_TRACE) collect \
		--profile dotnet-sampled-thread-time \
		--output "$(PROFILE_BASE).nettrace" \
		--show-child-io \
		-- dotnet "$(MONTE_CARLO_DLL)" \
			--runs 1 --ticks $(PROFILE_TICKS) --seed $(PROFILE_SEED) \
			--workers $(MC_WORKERS) \
			--world $(PROFILE_WORLD) \
			--run-id $(PROFILE_RUN_ID) --artifact-root .artifacts/monte-carlo \
			--simulation-only --no-analysis \
		2>&1 | tee "$(PROFILE_BASE).run.log"
	$(DOTNET_TRACE) report "$(PROFILE_BASE).nettrace" topN -n 120 -v \
		> "$(PROFILE_BASE).exclusive120.txt"
	$(DOTNET_TRACE) report "$(PROFILE_BASE).nettrace" topN --inclusive -n 180 -v \
		> "$(PROFILE_BASE).inclusive180.txt"
# Fan a batch across one process per seed so wall time is the slowest seed,
# not the sum, and each seed's data lands as soon as it finishes. Streams
# core scorecard tables to <run-id>-s<seed>/stream/ every MC_STREAM_EVERY
# ticks for mid-run analysis. Skips the per-run analyzer (score externally).
# Example: make mc-fan MC_TICKS=20000 MC_SEEDS="9100 9101 9102" RUN_ID=my-question
MC_SEEDS ?= 9100 9101 9102
MC_STREAM_EVERY ?= 2500
mc-fan:
	dotnet build tools/MonteCarlo/MonteCarlo.csproj -c Release
	for seed in $(MC_SEEDS); do \
		dotnet run --no-build --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
			--runs 1 --ticks $(MC_TICKS) --seed $$seed \
			--workers $(MC_WORKERS) \
			--run-id $(RUN_ID)-s$$seed --artifact-root .artifacts/monte-carlo \
			--no-analysis --stream-core-every $(MC_STREAM_EVERY) $(MC_EXTRA) & \
	done; wait

# Generate one pregenerated world: make world SEED=4200
world:
	dotnet run --project tools/WorldGenerator -c Release -- \
		--seed=$(SEED) --output=worlds/ice-remembers-seed-$(SEED).world

# Captures (written to .artifacts/captures/seed<SEED>/): make capture SEED=4200
capture:
	dotnet run --project tools/WorldGenerator -c Release -- \
		capture --world=worlds/ice-remembers-seed-$(SEED).world

# Worldgen diagnostics: make diagnose SEED=42 DIAG_FLAGS=--hex-contract
# (empty DIAG_FLAGS runs the full generation diagnostic dump)
diagnose:
	dotnet run --project tools/WorldGenerator -c Release -- \
		diagnose --seed=$(SEED) $(DIAG_FLAGS)

# Scoped fast suites - verify what the change touched, not everything:
test-kernel:
	dotnet test tests/Engine.Kernel.Tests --nologo \
		--settings test.runsettings --maxcpucount:1 --filter "$(FAST_FILTER)"

test-domain:
	dotnet test tests/Engine.Domain.Tests --nologo \
		--settings test.runsettings --maxcpucount:1 --filter "$(FAST_FILTER)"

test-eventing:
	dotnet test tests/Engine.Eventing.Tests --nologo \
		--settings test.runsettings --maxcpucount:1 --filter "$(FAST_FILTER)"

test-seed:
	dotnet test tests/IceRemembers.Seed.Tests --nologo \
		--settings test.runsettings --maxcpucount:1 --filter "$(FAST_FILTER)"

test-gameplay:
	dotnet test tests/Engine.Gameplay.Tests --nologo \
		--settings test.runsettings --maxcpucount:1 --filter "$(FAST_FILTER)"

clean:
	rm -rf .artifacts
	dotnet clean -c Release
