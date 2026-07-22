RESTORE_FLAGS ?= /p:NuGetAudit=false

# Tests tagged [Trait("Category", "Slow")] build full worlds, kernels, domain
# configurations, or Monte Carlo batches. They dominate wall time, so the
# default loop skips them; run `make ci-full` before pushing anything that
# touches worldgen, startup, wiring, or the generator catalog.
SLOW_FILTER ?= Category=Slow
FAST_FILTER ?= Category!=Slow

.PHONY: ci ci-full build test test-slow test-all lint analyzer-lint format-check clean domain-check mutation

ci: lint test domain-check

ci-full: lint test-all domain-check

build:
	dotnet restore $(RESTORE_FLAGS)
	dotnet build -c Release --no-restore

test:
	dotnet test -c Release --no-build --verbosity normal --filter "$(FAST_FILTER)"

test-slow:
	dotnet test -c Release --no-build --verbosity normal --filter "$(SLOW_FILTER)"

test-all:
	dotnet test -c Release --no-build --verbosity normal

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
MC_TICKS ?= 1000
MC_SEED ?= 1000
MC_EXTRA ?=
RUN_ID ?= dev-$(shell date +%Y%m%d-%H%M%S)
SEED ?= 42
DIAG_FLAGS ?= --hex-contract

.PHONY: mc world capture diagnose test-kernel test-domain test-seed test-gameplay

# Monte Carlo batch. Sizing guide: quick "did the number move" check =
# MC_RUNS=2; calibration report = MC_RUNS=6+ at the horizon you are judging.
# Example: make mc MC_RUNS=2 MC_TICKS=25000 RUN_ID=my-question
mc:
	dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -c Release -- \
		--runs $(MC_RUNS) --ticks $(MC_TICKS) --seed $(MC_SEED) \
		--run-id $(RUN_ID) --artifact-root .artifacts/monte-carlo $(MC_EXTRA)

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
	dotnet test tests/Engine.Kernel.Tests --nologo --filter "$(FAST_FILTER)"

test-domain:
	dotnet test tests/Engine.Domain.Tests --nologo --filter "$(FAST_FILTER)"

test-seed:
	dotnet test tests/IceRemembers.Seed.Tests --nologo --filter "$(FAST_FILTER)"

test-gameplay:
	dotnet test tests/Engine.Gameplay.Tests --nologo --filter "$(FAST_FILTER)"

clean:
	rm -rf .artifacts
	dotnet clean -c Release
