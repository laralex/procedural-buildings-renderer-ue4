﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ProceduralBuildingsGeneration
{
    public abstract class GenerationParameters
    {
        public int Seed { get; set; }
        public Random RandomGenerator { get; set; }
    }
}