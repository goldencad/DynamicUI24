# Privacy States

Privacy uses semantic icons `Privacy`, `PrivacyOn`, `PrivacyOff`, `PrivacyAuto`, `Reveal`, `Hide`, and `Restricted`, plus theme resources `Privacy.Mask.Foreground`, `Privacy.Mask.Background`, `Privacy.Hidden.Foreground`, `Privacy.Icon.Foreground`, `Privacy.Reveal.Focus`, and `Privacy.Restricted.Indicator`.

Mask is a fixed placeholder (`••••••••`) by default and does not reveal source length. Partial mask uses declared prefix/suffix preservation. Hide uses localized “Hidden”/“Đã ẩn” and a semantic icon where helpful. Privacy state is never conveyed by color alone.

Controls support System, Light, and Dark themes, existing UI/font scale, focus indication, and keyboard activation. Focus, selection, hover, edit entry, scrolling, theme, culture, or scale changes never reveal content. Protected tooltips are empty and automation receives “Sensitive value hidden” (localized) unless raw accessibility is explicitly permitted while revealed.
