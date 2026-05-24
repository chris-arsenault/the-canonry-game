RESTORE_FLAGS ?= /p:NuGetAudit=false

.PHONY: ci build test lint analyzer-lint format-check clean domain-check

ci: lint test domain-check

build:
	dotnet restore $(RESTORE_FLAGS)
	dotnet build -c Release --no-restore

test:
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

clean:
	rm -rf .artifacts
	dotnet clean -c Release
