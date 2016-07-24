using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Three {
	public static class Util {

		public static string ColorToHex(Color32 color) {
			string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
			return hex;
		}

		public static string ToJSONArray(string[] strings) {
			return "[" + string.Join (",", strings) + "]";
		}
		
	}
}