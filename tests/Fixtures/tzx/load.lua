-- Shared tape run. Each game script includes this and calls LoadTape.
-- filename is resolved beside the script that was started.
-- pc is the checkpoint address. bytes is the raw memory that must be there.
-- keys, bootFrames, and settleFrames are optional. The 128 Tape Loader
-- is selected with LoadTape(..., "<ENTER>", 400, 50).
function LoadTape(filename, machineId, pc, bytes, keys, bootFrames, settleFrames)
    if keys == nil then
        keys = "j+<SS>pp-<SS><ENTER>"
    end
    if bootFrames == nil then
        bootFrames = 100
    end
    if settleFrames == nil then
        settleFrames = 0
    end
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
    Emulator.WaitFrames(bootFrames)
    Tape.AutoPlay = false
    Tape.UseTraps = false
    Emulator.Load(filename)
    Debugger.AddBreakpoint(pc, bp)
    Debugger.AddBreakpoint(0x0000, reset)
    Emulator.MaximumSpeed = true
    Emulator.SendKeys(keys)
    if settleFrames > 0 then
        Emulator.WaitFrames(settleFrames)
    end
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
