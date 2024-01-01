using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WheelsGodot.discord {
    public static class TextManipulation {
        public static string ListItems(IEnumerable<string> items) {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var item in items) {
                if (i > 0) {
                    if (i == items.Count() - 1) {
                        sb.Append(i == 1 ? " and " : ", and ");
                    } else {
                        sb.Append(", ");
                    }
                }
                sb.Append(item);
                i++;
            }
            return sb.ToString();
        }

        public static string Possessivize(string name) {
            if (name.EndsWith("s")) {
                return name + "'";
            } else {
                return name + "'s";
            }
        }
    }
}
