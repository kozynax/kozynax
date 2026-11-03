using System;

namespace ZXMAK2.Hardware.Magic;

public class RcInt
{
    private readonly int _targetIntLength;
    private static readonly Random s_intRng = new Random();
    private int _framesUntilResample;

    public int IntLength { get; private set; }
            
    public RcInt(int targetIntLength)
    {
        _targetIntLength = targetIntLength;
        IntLength = targetIntLength;
    }

    public void OnBeginFrame()
    {
        if (_framesUntilResample <= 0)
        {
            // Changing once per second
            _framesUntilResample = 50;

            // Some stochastic heuristics according to minfo.tap
            // All this is dependent on RC chain parameters so it might be replaced with something more strict. 
            // Values are 34 (mostly) and 35 (a bit more rare)
            // Also sometimes it shows 0, 20% seems good approximation for this.
            var rnd = s_intRng.Next(100);
            /* if (rnd < 20) // disable 0 (skipped INT) temporarily as it breaks ROM keyboard read. Need to check the waveform on the actual hardware.
                IntLength = 1;
            else*/ if (rnd < 55)
                IntLength = _targetIntLength + 1;
            else
                IntLength = _targetIntLength;
        }
        else
        {
            _framesUntilResample--;
        }                
    }
}
