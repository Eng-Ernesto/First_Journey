using System;
using System.Threading.Tasks;
using UnityEngine;
using FirstJourney;
public static class AutomationReadinessTest
{
    public static async Task<object> FramesAdvance()
    {
        if(!Application.isPlaying)throw new Exception("Readiness test requires Play.");
        int first=Time.frameCount;var deadline=DateTime.UtcNow.AddSeconds(4);
        while(Time.frameCount-first<5||!FJWorldSession.Current||!FJWorldSession.Current.IsReady)
        {
            if(DateTime.UtcNow>deadline)throw new Exception("FJ-011: Play frames/session did not advance; first="+first+", last="+Time.frameCount+", runInBackground="+Application.runInBackground);
            await Task.Delay(16);
        }
        return new{passed=true,framesAdvanced=Time.frameCount-first,ready=FJWorldSession.Current.IsReady};
    }
}
