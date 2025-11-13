namespace GrapheneTrace.Client.Theme
{
    public static class AppStyles
    {
        public const string BtnMain =
            $"{AppColors.BgPrimary} {AppColors.TextOnPrimary} " +
            $"w-72 h-14 rounded-xl font-bold text-lg tracking-wider " +
            $"shadow-xl border-2 border-blue-800 " +
            $"{AppColors.HoverBgPrimary} " +
            $"transition duration-200 ease-in-out";

        public const string BtnSecondary =
            "bg-gray-200 text-gray-700 " +
            "w-72 h-14 rounded-xl font-semibold text-lg " +
            "shadow-md border border-gray-400 " +
            "hover:bg-gray-300 " +
            "transition duration-200 ease-in-out";

        public const string InputFormDefault =
            "p-4 border border-gray-300 " +
            "w-72 h-14 rounded-xl shadow-md " +
            $"text-base {AppColors.TextDefault} placeholder-gray-400 " +
            $"focus:ring-2 focus:ring-blue-500 focus:border-blue-500 " +
            "transition duration-150";

        public const string CardFloating =
            "bg-white p-8 sm:p-10 md:p-12 " +
            "shadow-2xl rounded-3xl " +
            "border border-gray-100 " +
            "w-full max-w-xl mx-auto";
    }
}
