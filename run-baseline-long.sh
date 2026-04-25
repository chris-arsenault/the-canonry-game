#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${REPO_ROOT}"

BATCH_COUNT="${BATCH_COUNT:-12}"
RUNS_PER_BATCH="${RUNS_PER_BATCH:-200}"
TICKS_PER_RUN="${TICKS_PER_RUN:-1000}"
START_SEED="${START_SEED:-1000}"
OUTPUT_ROOT="${OUTPUT_ROOT:-.artifacts/monte-carlo}"
LOG_PATH="${LOG_PATH:-${OUTPUT_ROOT}/baseline-long.console.log}"

mkdir -p "${OUTPUT_ROOT}"
export BATCH_COUNT RUNS_PER_BATCH TICKS_PER_RUN START_SEED OUTPUT_ROOT

stdbuf -oL -eL bash -lc '
set -euo pipefail

for batch in $(seq 0 $((BATCH_COUNT - 1))); do
  seed=$((START_SEED + batch * RUNS_PER_BATCH))
  out="${OUTPUT_ROOT}/baseline-long-batch-${batch}"
  echo "=== batch ${batch} seed=${seed} out=${out} ==="
  dotnet run --project tools/MonteCarlo/MonteCarlo.csproj -- \
    --runs "${RUNS_PER_BATCH}" \
    --ticks "${TICKS_PER_RUN}" \
    --seed "${seed}" \
    --out "${out}" || exit 1
done
' 2>&1 | tee "${LOG_PATH}"
