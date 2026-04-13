.PHONY: ci build test lint clean

ci: lint build test

build:
	dotnet restore
	dotnet build -c Release --no-restore

test:
	dotnet test -c Release --no-build --verbosity normal

lint:
	dotnet build -c Release -warnaserror

clean:
	rm -rf .artifacts
	dotnet clean -c Release
