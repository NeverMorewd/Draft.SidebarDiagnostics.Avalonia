# Localization

English is the neutral fallback. Simplified Chinese is provided in `src/Localization/Strings.zh-Hans.resx`. Select a language in Settings and save. The choice is persisted with the existing source-generated settings serializer.

Window controls use dynamic Avalonia resource references populated from the standard .NET ResourceManager. UI language does not replace the operating system's numeric/date formatting culture. Translations must not change metric identities, hardware names, units, custom names, JSON paths, or persisted enum values. Native device names and provider diagnostic messages may remain in their original language.

Add neutral text to `Strings.resx`, use the same key in satellite resources, and reference it with `DynamicResource` in XAML or `UiText.Get` in C#. Use `UiText.Format` for parameterized messages. Keep resource keys stable when editing copy. Missing translations fall back to English.

The first increment localizes static window and tray controls, common built-in metric labels, and key statuses. Additional upstream languages, provider error messages, and complete live relabeling of already-open charts remain follow-up work. Reopen a chart after changing language to refresh its duration choices.

Ship the culture subdirectories produced by dotnet publish along with the application, including for Native AOT packages. Do not discard satellite resource assemblies during packaging.
