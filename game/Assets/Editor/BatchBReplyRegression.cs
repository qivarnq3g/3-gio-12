using System.Collections.Generic;
using System.Linq;
using BaGio12.Prototype;
using BaGio12.Story;
using UnityEditor;
using UnityEngine;

public static class BatchBReplyRegression
{
    public static void Run()
    {
        var failures = new List<string>();
        var dialogue = JsonUtility.FromJson<NightDialogue>(Resources.Load<TextAsset>("Story/night-one-dialogue").text);
        if (string.IsNullOrEmpty(dialogue.batchBSilent) || dialogue.batchBSilent == dialogue.batchB) failures.Add("silent batch B line missing");
        if (NightOneDirector.BatchBOpening(dialogue, new List<string>()) != dialogue.batchBSilent) failures.Add("director thanked a silent player");
        if (NightOneDirector.BatchBOpening(dialogue, new List<string> { "camera:yes" }) != dialogue.batchB) failures.Add("director skipped thanks after a reply");
        if (NightOneDirector.BatchBOpening(dialogue, new List<string> { "call" }) != dialogue.batchB) failures.Add("director ignored a call");

        var data = JsonUtility.FromJson<PrototypeData>(Resources.Load<TextAsset>("Prototype/dem-1").text);
        var mom = data.contacts.FirstOrDefault(c => c.id == "me_ngan");
        if (mom == null) failures.Add("me_ngan contact missing");
        else
        {
            List<string> Lines(ICollection<string> sent) => mom.messages
                .Where(m => m.batch == "B" && string.IsNullOrEmpty(m.answers) && PhonePrototype.ArrivesFor(m, mom.id, sent))
                .Select(m => m.line).ToList();
            var silent = Lines(new HashSet<string>());
            var replied = Lines(new HashSet<string> { "me_ngan/camera:yes" });
            var called = Lines(new HashSet<string> { "me_ngan/call" });
            if (!silent.Contains("batchBSilent") || silent.Contains("batchB")) failures.Add("phone thanked a silent player: " + string.Join(",", silent));
            if (!replied.Contains("batchB") || replied.Contains("batchBSilent")) failures.Add("phone skipped thanks after a reply: " + string.Join(",", replied));
            if (!called.Contains("batchB")) failures.Add("phone ignored a call");
            if (!silent.Contains("warning") || !replied.Contains("warning")) failures.Add("warning must arrive either way");
        }

        if (failures.Count == 0) Debug.Log("BATCH_B_REPLY_REGRESSION_PASSED");
        else Debug.LogError("BATCH_B_REPLY_REGRESSION_FAILED " + string.Join(" | ", failures));
        EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }
}
