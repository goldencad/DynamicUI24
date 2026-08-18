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
