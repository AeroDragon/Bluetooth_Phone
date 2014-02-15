﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothClientWP8
{
    class Location
    {
        public string latitude { get; set; }
        public string longitude { get; set; }

        public Location()
        {

        }

        public Location(string latitude, string longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public override string ToString()
        {
            return String.Format("{0}{1}", latitude, longitude);
        }
    }
}