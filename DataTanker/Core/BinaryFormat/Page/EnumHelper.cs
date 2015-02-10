namespace DataTanker.BinaryFormat.Page
{
    using System.Collections.Generic;

    internal static class EnumHelper
    {
        public static FsmValue FsmValueFromSizeClass(SizeClass value)
        {
            switch (value)
            {
                case SizeClass.Class0: return FsmValue.Class0;
                case SizeClass.Class1: return FsmValue.Class1;
                case SizeClass.Class2: return FsmValue.Class2;
                case SizeClass.Class3: return FsmValue.Class3;
                case SizeClass.Class4: return FsmValue.Class4;
                case SizeClass.Class5: return FsmValue.Class5;
                case SizeClass.Class6: return FsmValue.Class6;
                case SizeClass.Class7: return FsmValue.Class7;
                case SizeClass.Class8: return FsmValue.Class8;
                case SizeClass.Class9: return FsmValue.Class9;
                case SizeClass.Class10: return FsmValue.Class10;
                case SizeClass.Class11: return FsmValue.Class11;

                default: return FsmValue.Full;
            }
        }

        public static IEnumerable<SizeClass> FixedSizeItemsSizeClasses()
        {
            yield return SizeClass.Class0;
            yield return SizeClass.Class1;
            yield return SizeClass.Class2;
            yield return SizeClass.Class3;
            yield return SizeClass.Class4;
            yield return SizeClass.Class5;
            yield return SizeClass.Class6;
            yield return SizeClass.Class7;
            yield return SizeClass.Class8;
            yield return SizeClass.Class9;
            yield return SizeClass.Class10;
            yield return SizeClass.Class11;
        }

        public static IEnumerable<SizeClass> AllSizeClasses()
        {
            yield return SizeClass.Class0;
            yield return SizeClass.Class1;
            yield return SizeClass.Class2;
            yield return SizeClass.Class3;
            yield return SizeClass.Class4;
            yield return SizeClass.Class5;
            yield return SizeClass.Class6;
            yield return SizeClass.Class7;
            yield return SizeClass.Class8;
            yield return SizeClass.Class9;
            yield return SizeClass.Class10;
            yield return SizeClass.Class11;

            yield return SizeClass.MultiPage;
            yield return SizeClass.NotApplicable;
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