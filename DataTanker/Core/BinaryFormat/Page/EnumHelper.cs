namespace DataTanker.BinaryFormat.Page
{
    using System.Collections.Generic;

    internal static class EnumHelper
    {
        public static FsmValue FsmValueFromSizeRange(SizeRange value)
        {
            switch (value)
            {
                case SizeRange.Range0: return FsmValue.Class0;
                case SizeRange.Range1: return FsmValue.Class1;
                case SizeRange.Range2: return FsmValue.Class2;
                case SizeRange.Range3: return FsmValue.Class3;
                case SizeRange.Range4: return FsmValue.Class4;
                case SizeRange.Range5: return FsmValue.Class5;
                case SizeRange.Range6: return FsmValue.Class6;
                case SizeRange.Range7: return FsmValue.Class7;
                case SizeRange.Range8: return FsmValue.Class8;
                case SizeRange.Range9: return FsmValue.Class9;
                case SizeRange.Range10: return FsmValue.Class10;
                case SizeRange.Range11: return FsmValue.Class11;

                default: return FsmValue.Full;
            }
        }

        public static IEnumerable<SizeRange> FixedSizeItemsSizeRanges()
        {
            yield return SizeRange.Range0;
            yield return SizeRange.Range1;
            yield return SizeRange.Range2;
            yield return SizeRange.Range3;
            yield return SizeRange.Range4;
            yield return SizeRange.Range5;
            yield return SizeRange.Range6;
            yield return SizeRange.Range7;
            yield return SizeRange.Range8;
            yield return SizeRange.Range9;
            yield return SizeRange.Range10;
            yield return SizeRange.Range11;
        }

        public static IEnumerable<SizeRange> AllSizeRanges()
        {
            yield return SizeRange.Range0;
            yield return SizeRange.Range1;
            yield return SizeRange.Range2;
            yield return SizeRange.Range3;
            yield return SizeRange.Range4;
            yield return SizeRange.Range5;
            yield return SizeRange.Range6;
            yield return SizeRange.Range7;
            yield return SizeRange.Range8;
            yield return SizeRange.Range9;
            yield return SizeRange.Range10;
            yield return SizeRange.Range11;

            yield return SizeRange.MultiPage;
            yield return SizeRange.NotApplicable;
        }

        public static IEnumerable<FsmValue> FixedSizeItemsFsmValues()
        {
            yield return FsmValue.Class0;
            yield return FsmValue.Class1;
            yield return FsmValue.Class2;
            yield return FsmValue.Class3;
            yield return FsmValue.Class4;
            yield return FsmValue.Class5;
            yield return FsmValue.Class6;
            yield return FsmValue.Class7;
            yield return FsmValue.Class8;
            yield return FsmValue.Class9;
            yield return FsmValue.Class10;
            yield return FsmValue.Class11;
        }

        public static IEnumerable<FsmValue> AllFsmValues()
        {
            yield return FsmValue.Class0;
            yield return FsmValue.Class1;
            yield return FsmValue.Class2;
            yield return FsmValue.Class3;
            yield return FsmValue.Class4;
            yield return FsmValue.Class5;
            yield return FsmValue.Class6;
            yield return FsmValue.Class7;
            yield return FsmValue.Class8;
            yield return FsmValue.Class9;
            yield return FsmValue.Class10;
            yield return FsmValue.Class11;

            yield return FsmValue.Reserved1;
            yield return FsmValue.Reserved2;
            yield return FsmValue.Reserved3;

            yield return FsmValue.Full;
        }
    }
}