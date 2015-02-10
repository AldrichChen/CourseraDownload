using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace CourseraDownloader.Model {
    public class CourseElement : ConfigurationElement {
        public CourseElement(string newName, string newSchool, int newWeek, string newUrl) {

        }

        public CourseElement() {

        }

        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name {
            get { return this["name"] as string; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("school", IsRequired = true)]
        public string School {
            get { return this["school"] as string; }
            set { this["school"] = value; }
        }

        [ConfigurationProperty("week", IsRequired = true)]
        public string Week {
            get { return this["week"] as string; }
            set { this["week"] = value; }
        }

        [ConfigurationProperty("url", IsRequired = true)]
        public string Url {
            get { return this["url"] as string; }
            set { this["url"] = value; }
        }
    }

    public class CourseCollection : ConfigurationElementCollection {
        public CourseElement this[int index] {
            get {
                return base.BaseGet(index) as CourseElement;
            }
        }

        protected override ConfigurationElement CreateNewElement() {
            return new CourseElement();
        }

        /**
        protected override ConfigurationElement CreateNewElement(string newName, string newSchool, int newWeek, string newUrl) {
            return new CourseElement(newName, newSchool, newWeek, newUrl);
        }
         */

        protected override object GetElementKey(ConfigurationElement element) {
            return ((CourseElement)(element)).Name;
        }

        public int GetElementIndex(string name) {
            for (int i = 0; i < this.Count; i++) {
                if (this[i].Name == name) {
                    return i;
                }
            }
            return -1;
        }
    }

    public class CourseSection : ConfigurationSection {
        public CourseSection() {

        }

        [ConfigurationProperty("Courses")]
        public CourseCollection Courses {
            get {
                return this["Courses"] as CourseCollection;
            }
        }

        public static CourseSection GetConfigSection() {
            CourseSection course = ConfigurationManager.GetSection("CourseSection") as CourseSection;
            return course;
        }
    }

}
