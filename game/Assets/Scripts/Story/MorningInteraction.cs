using BaGio12.Gameplay;

namespace BaGio12.Story
{
    public sealed class MorningInteraction : WorldInteractable
    {
        public NightTwoMorning owner;
        public string action;
        public int index;
        public override string Prompt => owner.PromptFor(action, index);
        public override void Interact(FirstPersonPlayer player) => owner.Interact(action, index);
    }
}
