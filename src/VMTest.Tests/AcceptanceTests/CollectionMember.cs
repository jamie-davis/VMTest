using System.Collections.ObjectModel;
using System.Linq;

namespace VMTest.Tests.AcceptanceTests
{
    internal class CollectionMember : AcceptanceTestVM
    {
        private string _name;
        private int _count;

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public int Count
        {
            get { return _count; }
            set
            {
                if (value == _count) return;
                _count = value;
                OnPropertyChanged("Count");
            }
        }

        public static void InitialiseMembersCollection(ObservableCollection<CollectionMember> inMembers)
        {
            {
                var members = new[]
                {
                    "Alpha", "Beta", "Charlie",
                    "Delta", "Echo", "Hotel",
                    "Indigo", "Juliette", "Kilo",
                    "Lima", "Mike", "November",
                    "Oscar", "Papa", "Quebec",
                    "Romeo", "Sierra", "Tango",
                    "Uniform", "Victor", "Whiskey",
                    "X-Ray", "Zulu"
                }
                    .Select(n => new CollectionMember
                    {Name = n, Count = n.Length});
                foreach (var member in members)
                {
                    inMembers.Add(member);
                }
            }
        }
    }
}