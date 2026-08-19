using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using DynamicUI24.Avalonia.Presentation.Editors;
using DynamicUI24.Core.Editors;
using Xunit;

namespace DynamicUI24.Tests;

[Collection("Avalonia native UI")]
public sealed class UniversalEditorNativeInputTests
{
    [Theory]
    [InlineData(EditorValueType.String, null)]
    [InlineData(EditorValueType.LongString, null)]
    [InlineData(EditorValueType.String, EditorKind.ButtonEdit)]
    [InlineData(EditorValueType.Secret, null)]
    public void TextFamiliesDeferNativeOsInputActivationUntilVisualAttachment(EditorValueType valueType, EditorKind? kind)
    {
        var presenter = Create(valueType, kind);
        var text = Assert.Single(presenter.NativeTextInputs);
        Assert.True(InputMethod.GetIsInputMethodEnabled(text));
        Assert.False(NativeEditorInputOwnership.IsLifecycleReady(text));
    }

    [Fact]
    public async Task TextChangedPreEditDoesNotPromoteCandidateUntilCommit()
    {
        var definition = Definition(EditorValueType.String);
        var state = new EditorRuntimeState(definition, "Tiếng Việt cũ");
        var presenter = Create(definition, state);
        var text = Assert.IsType<TextBox>(presenter.NativeEditor);

        text.Text = "Tiếng Việt mới: ắ ằ ẵ ặ 🌏";
        Assert.Equal("Tiếng Việt cũ", state.CandidateValue);
        Assert.Equal("Tiếng Việt cũ", state.CommittedValue);

        Assert.True(await presenter.CommitAsync());
        Assert.Equal("Tiếng Việt mới: ắ ằ ẵ ặ 🌏", state.CandidateValue);
        Assert.Equal("Tiếng Việt mới: ắ ằ ẵ ặ 🌏", state.CommittedValue);
    }

    [Fact]
    public void ExistingUnicodeValueLoadsWithoutNormalizationAndKeepsNativePasswordModeOff()
    {
        const string value = "Cộng hòa xã hội chủ nghĩa Việt Nam 🇻🇳";
        var definition = Definition(EditorValueType.String);
        var presenter = Create(definition, new(definition, value));
        var text = Assert.IsType<TextBox>(presenter.NativeEditor);
        Assert.Equal(value, text.Text);
        Assert.Equal(new TextBox().PasswordChar, text.PasswordChar);
    }

    [Fact]
    public void ParentRoutingRecognizesNativeTextOwnership()
    {
        Assert.True(NativeEditorInputOwnership.Owns(new TextBox()));
        Assert.False(NativeEditorInputOwnership.Owns(new Button()));
    }

    [Fact]
    public void LookupSearchUsesTheSameNativeInputBoundary()
    {
        var presenter = new AvaloniaEditorPresenter(Definition(EditorValueType.LookupKey, EditorKind.SearchLookup),
            new(Definition(EditorValueType.LookupKey, EditorKind.SearchLookup)),
            new EditorResolver().Resolve(Definition(EditorValueType.LookupKey, EditorKind.SearchLookup),
                EditorPlatformCapabilities.AllNative), CultureInfo.InvariantCulture, lookupProvider: new EmptyLookupProvider());
        var query = Assert.Single(presenter.NativeTextInputs);
        Assert.True(InputMethod.GetIsInputMethodEnabled(query));
        Assert.False(NativeEditorInputOwnership.IsLifecycleReady(query));
    }

    [Fact]
    public void NumericTextualEntryEnablesNativeInputMethod()
    {
        var presenter = Create(EditorValueType.Decimal);
        Assert.True(InputMethod.GetIsInputMethodEnabled(Assert.IsType<NumericUpDown>(presenter.NativeEditor)));
    }

    [Fact]
    public async Task Date_is_one_compact_calendar_field_and_retains_DateOnly_semantics()
    {
        var definition = Definition(EditorValueType.Date);
        var state = new EditorRuntimeState(definition, new DateOnly(2026, 8, 18));
        var presenter = Create(definition, state);
        var picker = Assert.IsType<CalendarDatePicker>(presenter.NativeEditor);
        Assert.Equal("dd/MM/yyyy", picker.CustomDateFormatString);
        Assert.Equal(EditorPresentationTokens.CompactControlWidth, picker.Width);
        picker.SelectedDate = new DateTime(2026, 8, 19);
        Assert.True(await presenter.CommitAsync());
        Assert.Equal(new DateOnly(2026, 8, 19), state.CommittedValue);
    }

    [Fact]
    public async Task Time_is_one_native_text_field_and_retains_TimeOnly_semantics()
    {
        var definition = Definition(EditorValueType.Time);
        var state = new EditorRuntimeState(definition, new TimeOnly(9, 30));
        var presenter = Create(definition, state);
        var time = Assert.IsType<TextBox>(presenter.NativeEditor);
        Assert.Equal("09:30", time.Text);
        Assert.True(InputMethod.GetIsInputMethodEnabled(time));
        time.Text = "14:45";
        Assert.True(await presenter.CommitAsync());
        Assert.Equal(new TimeOnly(14, 45), state.CommittedValue);
    }

    [Fact]
    public async Task DateTime_and_DateRange_are_compact_semantic_compositions()
    {
        var dateTimeDefinition = Definition(EditorValueType.DateTime);
        var dateTimeState = new EditorRuntimeState(dateTimeDefinition, new DateTime(2026, 8, 18, 9, 30, 0));
        var dateTime = Create(dateTimeDefinition, dateTimeState);
        var dateTimeGrid = Assert.IsType<Grid>(dateTime.NativeEditor);
        Assert.Single(dateTimeGrid.Children.OfType<CalendarDatePicker>());
        Assert.Single(dateTimeGrid.Children.OfType<TextBox>());
        Assert.True(await dateTime.CommitAsync());
        Assert.Equal(new DateTime(2026, 8, 18, 9, 30, 0), dateTimeState.CommittedValue);

        var rangeDefinition = Definition(EditorValueType.DateRange);
        var original = new DateRangeValue(new(2026, 8, 1), new(2026, 8, 31));
        var rangeState = new EditorRuntimeState(rangeDefinition, original);
        var range = Create(rangeDefinition, rangeState);
        var panel = Assert.IsType<WrapPanel>(range.NativeEditor);
        Assert.Equal(2, panel.Children.OfType<StackPanel>().SelectMany(x => x.Children).OfType<CalendarDatePicker>().Count());
        Assert.True(await range.CommitAsync());
        Assert.Equal(original, rangeState.CommittedValue);
    }

    [Fact]
    public void Culture_refresh_changes_only_date_presentation_and_preserves_control_identity_and_accessibility()
    {
        var definition = new EditorDefinition(new("NATIVE.DATE"), new("NATIVE.FIELD"), EditorValueType.Date,
            helpContextCode: new("HELP.DATE"), chrome: new(LabelKey: new("Date.Label")));
        var state = new EditorRuntimeState(definition, new DateOnly(2026, 8, 18));
        var presenter = Create(definition, state);
        var picker = Assert.IsType<CalendarDatePicker>(presenter.NativeEditor);
        presenter.RefreshLocalizedPresentation(CultureInfo.GetCultureInfo("en-US"), key => key.Value == "Date.Label" ? "Date" : key.Value);
        Assert.Same(picker, presenter.NativeEditor);
        Assert.Equal("M/d/yyyy", picker.CustomDateFormatString);
        Assert.Equal("Date", global::Avalonia.Automation.AutomationProperties.GetName(picker));
        Assert.Equal(new DateOnly(2026, 8, 18), state.CandidateValue);
        Assert.Equal(new("HELP.DATE"), definition.HelpContextCode);
    }

    private static AvaloniaEditorPresenter Create(EditorValueType valueType, EditorKind? kind = null)
    {
        var definition = Definition(valueType, kind);
        return Create(definition, new(definition));
    }

    private static AvaloniaEditorPresenter Create(EditorDefinition definition, EditorRuntimeState state) =>
        new(definition, state, new EditorResolver().Resolve(definition, EditorPlatformCapabilities.AllNative),
            CultureInfo.GetCultureInfo("vi-VN"));

    private static EditorDefinition Definition(EditorValueType valueType, EditorKind? kind = null) =>
        new(new($"NATIVE.{valueType}.{kind}"), new("NATIVE.FIELD"), valueType, kind);

    private sealed class EmptyLookupProvider : IEditorLookupProvider
    {
        public string ProviderCode => "EMPTY";
        public ValueTask<EditorLookupResult> QueryAsync(EditorLookupRequest request) => ValueTask.FromResult(
            new EditorLookupResult(ImmutableArray<EditorLookupOption>.Empty, null, 0, request.Generation,
                request.CompanyId, request.ContextRevision, EditorLookupStatus.Empty));
    }
}
