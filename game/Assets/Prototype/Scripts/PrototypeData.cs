using System;
using System.Collections.Generic;

namespace BaGio12.Prototype
{
    // Shape of Resources/Prototype/dem-1.json and chat-mau.json.
    // Handles contacts, messages, choices, photos, and voice memos.
    //
    // Messages arrive in batches ("lô tin") that the game delivers on story events
    // (PhonePrototype.DeliverBatch). A message with no batch is there from the start.
    [Serializable]
    public class PrototypeData
    {
        public string note;
        public string scenarioId;
        public string lockTime;       // clock at the start of the scenario; the game moves it with SetClock
        public string lockDate;
        public string lockNotifyAgo;  // age shown on the lock-screen notification, e.g. "4 phút trước"
        public string chatDay;        // divider at the top of every chat, e.g. "Hôm nay - 3:12 AM"
        public string albumDay;       // section heading in Nimbo, e.g. "Hôm nay - Đêm 1"
        public bool noTyping;         // never show "đang soạn tin..." (Ngân's phone, luật 6)
        public List<string> startBatches = new List<string>();  // batches already in the phone when it is picked up
        public List<Contact> contacts = new List<Contact>();
        public List<Photo> photos = new List<Photo>();
        public List<VoiceMemo> voiceMemos = new List<VoiceMemo>();
    }

    [Serializable]
    public class Contact
    {
        public string id;
        public string name;
        public string preview;
        public string status;     // line under the name in the chat header; empty = "Đang hoạt động"
        public bool autoRead;     // messages count as read the moment they arrive (dãy số lạ, đêm 1)
        public string callNote;   // shown in the chat after calling; empty = the call button does nothing
        public string callSets;   // flag set by calling, same format as Choice.sets
        public List<Message> messages = new List<Message>();
        public List<Choice> choices = new List<Choice>();
    }

    [Serializable]
    public class Message
    {
        public string from;     // "them", "me" or "system" (centred note such as a call or a block)
        public string text;
        public string time;
        public float delay;     // seconds of "typing" before the message appears
        public string batch;    // batch it arrives with, e.g. "B"; empty = from the start
        public string answers;  // choice id (or "call") this line answers: it only arrives if the player sent
                                // that before the batch, and only in the first batch that answers it
    }

    [Serializable]
    public class Choice
    {
        public string id;
        public string text;
        public string group;     // choices in one group exclude each other; empty = one shared group per contact
        public string requires;  // flag that must be set before the choice is offered, e.g. "saw_shop_camera"
        public string sets;      // flag set when sent: "asked_mom_why_here" or "number_choice=blocked"
        public bool block;       // blocks the contact instead of sending a message
        public List<Message> replies = new List<Message>();  // answered at once (prototype only; story contacts use batches)
    }

    [Serializable]
    public class Photo
    {
        public string resource;  // path under a Resources folder, without extension
        public string caption;
        public string time;
    }

    [Serializable]
    public class VoiceMemo
    {
        public string id;
        public string title;
        public string duration;
        public string time;
        public string date;
        public string transcript;
        public string resource;
    }
}
