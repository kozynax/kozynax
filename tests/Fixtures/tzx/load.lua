-- Shared tape run. Each game script includes this and calls LoadTape.
-- filename is resolved beside the script that was started.
-- pc is the checkpoint address. bytes is the raw memory that must be there.
function LoadTape(filename, machineId, pc, bytes)
    local result = nil
    local function finish(ok)
        if result ~= nil then
            return
        end
        result = ok
    end
    local function bp(cpu)
        if Debugger.ReadMemory(cpu.regs.PC, #bytes) == bytes then
            finish(true)
        elseif cpu.regs.PC == 0 then
            finish(false)
        end
    end
    local function reset()
        finish(false)
    end

    Machine.Set(machineId)
    Machine.Reset()
    Emulator.WaitFrames(100)
    Tape.AutoPlay = false
    Tape.UseTraps = false
    Emulator.Load(filename)
    Debugger.AddBreakpoint(pc, bp)
    Debugger.AddBreakpoint(0x0000, reset)
    Emulator.MaximumSpeed = true
    Emulator.SendKeys("j+<SS>pp-<SS><ENTER>")
    Tape.Start()
    
    Emulator.WaitFrames(90000, function()
        return result ~= nil
    end)
    
    if result == nil then
        result = false
    end
    print(filename .. (result and " Success" or " Fail"))
    if Emulator.IsHeadless then
        Emulator.Exit(result and 0 or 1)
    end
end
