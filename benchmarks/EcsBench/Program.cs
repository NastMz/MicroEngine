using EcsBench;
using StressBench;

// ENTITY-1 (docs/evaluations/entity-1.md). The stress harness with the entity workloads added: every command and
// option of StressBench works here (run, sweep, list, compare), and the scenarios are named ecs-<workload>-<store>.
//
//   dotnet run -c Release --project benchmarks/EcsBench -- list
//   dotnet run -c Release --project benchmarks/EcsBench -- run --scenario ecs-move-simple,ecs-move-arch,ecs-move-friflo,ecs-move-proto --load 100000 --isolate
//   dotnet run -c Release --project benchmarks/EcsBench -- sweep --scenario ecs-mixed-simple --mode headless
EcsScenarios.Register();
return StressProgram.Run(args);
