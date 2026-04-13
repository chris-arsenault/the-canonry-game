.PHONY: ci build test lint clean domain-check

ci: lint build test domain-check

build:
	dotnet restore
	dotnet build -c Release --no-restore

test:
	dotnet test -c Release --no-build --verbosity normal

lint:
	dotnet build -c Release -warnaserror

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
