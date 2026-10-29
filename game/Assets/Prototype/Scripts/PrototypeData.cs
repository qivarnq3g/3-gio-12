using System;
using System.Collections.Generic;

namespace BaGio12.Prototype
{
    // Shape of Resources/Prototype/dem-1.json and chat-mau.json.
    // Handles contacts, messages, choices, photos, and voice memos.
    [Serializable]
    public class PrototypeData
    {
        public string note;
        public string scenarioId;
        public string lockTime;
        public string lockDate;
        public string chatDay;   // divider at the top of every chat, e.g. "Hôm nay - 3:12 AM"
        public string albumDay;  // section heading in Nimbo, e.g. "Hôm nay - Đêm 1"
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
        public List<Message> messages = new List<Message>();
        public List<Choice> choices = new List<Choice>();
    }

    [Serializable]
    public class Message
    {
        public string from;   // "them" or "me"
        public string text;
        public string time;
        public float delay;   // seconds of "typing" before the message appears
    }

    [Serializable]
    public class Choice
    {
        public string id;
        public string text;
        public List<Message> replies = new List<Message>();
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
