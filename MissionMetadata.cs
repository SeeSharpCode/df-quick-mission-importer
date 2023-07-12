namespace DFQuickMissionImporter
{
    public class MissionMetadata
    {
        public MissionMetadata() { }

        public MissionMetadata(string title, string quickBriefing, string longBriefing)
        {
            Title = title;
            QuickBriefing = quickBriefing;
            LongBriefing = longBriefing;
        }

        public string Title { get; set; }
        public string QuickBriefing { get; set; }
        public string LongBriefing { get; set; }
    }
}
