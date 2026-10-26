using System;
using System.Collections.Generic;

namespace BaGio12.Prototype
{
    // Shape of Resources/Prototype/chat-mau.json. Placeholder content only; the real
    // script format will live in the Story.Core library once the story is locked.
    [Serializable]
    public class PrototypeData
    {
        public string note;
        public string lockTime;
        public string lockDate;
        public List<Contact> contacts = new List<Contact>();
        public List<Photo> photos = new List<Photo>();
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
        public string text;
        public List<Message> replies = new List<Message>();
    }

    [Serializable]
    public class Photo
    {
        public string resource;  // path under a Resources folder, without extension
        public string caption;
    }
}
