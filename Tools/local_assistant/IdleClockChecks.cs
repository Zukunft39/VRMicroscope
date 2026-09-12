using System;
using VRMicroscope.Assistant;

internal static class IdleClockChecks
{
    private static int checks;
    private static void Check(bool value, string reason)
    {
        if (!value) throw new Exception(reason);
        checks++;
    }
    public static void Main()
    {
        var clock = new AssistantIdleClock();
        Check(!clock.Tick(29.9f,false,30,0),"Early reminder");
        Check(clock.Tick(.2f,false,30,0),"First reminder missing");
        Check(!clock.Tick(10,true,30,0),"Reminder while reading");
        Check(!clock.Tick(29,false,30,0),"Reading time leaked into idle interval");
        Check(clock.Tick(1,false,30,0),"Reminder after a fresh quiet interval missing");
        clock.Activity();
        clock.Tick(25,false,30,0);
        clock.Activity();
        Check(!clock.Tick(10,false,30,0),"Action failed to reset inactivity");
        Check(!clock.Tick(100,true,30,0),"Tutorial time leaked");
        Check(!clock.Tick(29,false,30,0),"Reminder immediately after tutorial");
        Check(clock.Tick(1,false,30,0),"Reminder after tutorial missing");
        clock.Activity();
        Check(clock.Tick(30,false,30,2),"Bounded first reminder missing");
        Check(clock.Tick(30,false,30,2),"Bounded second reminder missing");
        Check(!clock.Tick(300,false,30,2),"Configured burst limit ignored");
        clock.Activity();
        Check(clock.Tick(30,false,30,2),"Activity did not rearm burst");
        clock.Activity();
        for(int i=0;i<10;i++) Check(clock.Tick(30,false,30,0),"Unlimited reminders stopped");
        clock.Activity();
        Check(!clock.Tick(-20,false,30,0),"Negative elapsed time accepted");
        Check(!clock.Tick(0,false,30,0),"Zero elapsed time accepted");
        Check(clock.Tick(30,false,30,0),"Invalid delta corrupted timer");
        Console.WriteLine("Local assistant: "+checks+" timing checks passed.");
    }
}
