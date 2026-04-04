using System.ComponentModel; // Hedha l-namespace li fih l-INotifyPropertyChanged interface
using System.Runtime.CompilerServices; // hedha l-namespace li fih l-CallerMemberName attribute, li t5alli l-method t3raf isem l-property li 3am t3mel notify 3leha bl-automatic

namespace WpfLearning
{
    public class Person : INotifyPropertyChanged  // interface hedhy t5alli l-class t3raf t3mel notify l-property changes, w t5alli l-binding y3raf y-update l-UI
    {
        public event PropertyChangedEventHandler PropertyChanged;  // adheya houwa l bell ringer kima , testana lin tji m {Binding Name} w t-updati l UI

        private void Notify([CallerMemberName] string propertyName = null) // lhelper eli naamloulha call whenever any property changes
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); // hedha y3ni "if PropertyChanged is not null, then call it with the current object and the name of the property that changed"   
                                                                                       //L message eli yetba3 l-UI houwa PropertyChangedEventArgs, w fih isem l-property li 3am t3mel notify 3leha, w hedha y5alli l-UI t3raf ay property t-update 3leha
                                                                                       // l [callerMemberName] attribute t5alli l-method t3raf isem l-property li 3am t3mel notify 3leha bl-automatic, w ma b7taj n3mlha pass kima Notify(nameof(Name)) w Notify(nameof(City))
        }


        private string _name;
        public string Name
        {
            get => _name; // hedha houwa l-getter, w y3ni "return _name"
            set // hedha houwa l-setter, w y3ni "set _name to the value being assigned to Name"
            {
                _name = value; // hedha houwa l-backing field, w houwa variable private li y5alli l-property t3raf t-store l-value
                Notify();// [CallerMemberName] fills in "Name" automatically
                Notify(nameof(Introduction)); // hedha houwa l-UI update trigger, w y3ni "notify the UI that the Introduction property has changed, so it should update any bindings to Introduction"
            }
        }

        private string _city;
        public string City // kif mtaa name exactly, w ma b7taj n3mlha Notify(nameof(City)) 3ashan [CallerMemberName] y3raf t3mel notify l-city bl-automatic
        {
            get => _city;
            set
            {
                _city = value;
                Notify();
                Notify(nameof(Introduction));
            }
        }

        public string Introduction => $"Hi! I'm {_name} from {_city}."; // hedha houwa l-readonly property, w y3ni "return a string that introduces the person using their name and city". w ma b7taj setter 3leha 3ashan hiya computed property, w t3tamed 3la Name w City, w ma b7taj n3mlha notify 3ashan hiya t-update automatically kima Name w City
    }
}