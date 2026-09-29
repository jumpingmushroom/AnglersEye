using System;
using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class TargetPickerTests
    {
        private static readonly Vec3 Origin = new Vec3(0f, 2f, 0f);
        private static readonly Vec3 AimZ = new Vec3(0f, -0.3f, 1f);

        private static FishSighting At(string species, float x, float y, float z, int q = 1)
        {
            return new FishSighting(species, q, new Vec3(x, y, z));
        }

        private static FishSighting AtAngle(string species, float degrees, float dist)
        {
            double r = degrees * Math.PI / 180.0;
            return At(species, (float)(Math.Sin(r) * dist), 0f, (float)(Math.Cos(r) * dist));
        }

        private static FishSighting Pick(FishSighting cross, Vec3 aim, params FishSighting[] fish)
        {
            return TargetPicker.Pick(cross, Origin, aim, 10f, 30f, new List<FishSighting>(fish));
        }

        [Fact]
        public void Crosshair_Wins()
        {
            FishSighting cross = At("Pike", 50f, 0f, 50f);
            Assert.Same(cross, Pick(cross, AimZ, At("Perch", 0f, 0f, 5f)));
        }

        [Fact]
        public void NearestInCone_BeatsNearerFishOutsideIt()
        {
            FishSighting ahead = At("Pike", 0f, 0f, 15f);
            FishSighting side = At("Perch", 4f, 0f, 0f);
            Assert.Same(ahead, Pick(null, AimZ, side, ahead));
        }

        [Fact]
        public void ConeEdge_InsideIncluded_OutsideExcluded()
        {
            FishSighting inside = AtAngle("Pike", 9f, 20f);
            FishSighting outside = AtAngle("Perch", 11f, 5f);
            Assert.Same(inside, Pick(null, AimZ, outside, inside));
        }

        [Fact]
        public void OutOfRange_Ignored()
        {
            Assert.Null(Pick(null, AimZ, At("Pike", 0f, 0f, 40f)));
        }

        [Fact]
        public void NothingInCone_MostCommonSpeciesNearby()
        {
            FishSighting p1 = At("Perch", 0f, 0f, -10f);
            FishSighting p2 = At("Perch", 1f, 0f, -10f);
            FishSighting pike = At("Pike", 0f, 0f, -3f);
            Assert.Same(p1, Pick(null, AimZ, pike, p2, p1));
        }

        [Fact]
        public void NothingInCone_TieGoesToNearest()
        {
            FishSighting perch = At("Perch", 0f, 0f, -10f);
            FishSighting pike = At("Pike", 0f, 0f, -5f);
            Assert.Same(pike, Pick(null, AimZ, perch, pike));
        }

        [Fact]
        public void LookingStraightDown_SkipsConeUsesFallback()
        {
            FishSighting perch = At("Perch", 0f, 0f, 6f);
            Assert.Same(perch, Pick(null, new Vec3(0f, -1f, 0f), perch));
        }

        [Fact]
        public void FishDirectlyBelow_CountsAsInCone()
        {
            FishSighting below = At("Pike", 0f, -1f, 0f);
            FishSighting far = At("Perch", 0f, 0f, 20f);
            Assert.Same(below, Pick(null, AimZ, far, below));
        }

        [Fact]
        public void NoFish_ReturnsNull()
        {
            Assert.Null(Pick(null, AimZ));
        }
    }
}
