# CONTENT-1: what else the machine is doing, sampled over five seconds: the CPU time every process used meanwhile,
# the four busiest, and the machine. campaign.sh records it before and after every group of measurements.
$a = Get-Process | Select-Object Id, ProcessName, CPU
Start-Sleep -Seconds 5
$b = Get-Process | Select-Object Id, ProcessName, CPU
$busy = foreach ($p in $b) { $q = $a | Where-Object Id -eq $p.Id; if ($q -and $p.CPU -ne $null) { [pscustomobject]@{ Name = $p.ProcessName; Id = $p.Id; Cpu = [math]::Round($p.CPU - $q.CPU, 2) } } }
$total = ($busy | Measure-Object Cpu -Sum).Sum
"idle check $(Get-Date -Format s): other processes used $([math]::Round($total,2)) CPU-seconds in 5 s on $([Environment]::ProcessorCount) logical cores ($([math]::Round($total / 5 / [Environment]::ProcessorCount * 100, 1))% of the machine)"
$busy | Sort-Object Cpu -Descending | Select-Object -First 4 | ForEach-Object { "  $($_.Name) ($($_.Id)): $($_.Cpu) s" }
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
"cpu: $($cpu.Name.Trim()); memory: $([math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB)) GB; os: $((Get-CimInstance Win32_OperatingSystem).Caption) $((Get-CimInstance Win32_OperatingSystem).Version)"
(Get-CimInstance Win32_VideoController | ForEach-Object { "gpu: $($_.Name), driver $($_.DriverVersion)" })
