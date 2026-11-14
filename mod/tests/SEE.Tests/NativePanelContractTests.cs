using System;
using System.Collections.Generic;
using System.IO;
using SEE.UI.Panels;

namespace SEE.Tests
{
    /// <summary>
    /// Offline contract tests for the vanilla-styled boiler panel (REQ-UI-01/05/12).
    ///
    /// These are source-scan assertions plus one pure-logic check: they never touch a game
    /// runtime. The game assemblies the panel references cannot be loaded by the net9 test runner,
    /// so "the panel uses the vanilla USS classes and controls" is proven by asserting the exact
    /// class names and native API calls in the production sources. Together with
    /// <c>UiContractTests.SourceScanTests</c> they keep the panel from drifting back to an
    /// inline-styled, Unity-default-looking block.
    /// </summary>
    internal static class NativePanelContractTests
    {
        internal static void Run(TestRunner t)
        {
            CollapseTests(t);
            NativeClassTests(t);
            NativeControlTests(t);
            InlineStyleTests(t);
        }

        // ------------------------------------------------------------------ collapse block

        private static void CollapseTests(TestRunner t)
        {
            t.Run("The SEE block uses a native collapse header, not a Unity Foldout", () =>
            {
                foreach (string file in UiSourceFiles())
                {
                    string text = File.ReadAllText(file);
                    Assert.False(text.Contains("Foldout", StringComparison.Ordinal),
                        "Foldout must not be used in " + file);
                }

                string view = ViewSource();
                Assert.Contains(view, "PanelCollapseState", "the view owns the collapse state");
                Assert.Contains(view, "CreateCollapseHeader", "the header is a native clickable row");
                Assert.Contains(view, "ToggleCollapsed", "the header toggles the body");
            });

            t.Run("The SEE block is expanded by default and survives a render", () =>
            {
                var state = new PanelCollapseState();
                Assert.True(state.IsExpanded, "a new block starts expanded");
                Assert.False(state.Toggle(), "the first click collapses the block");
                Assert.True(state.Toggle(), "the second click expands it again");

                // The panel source is split into partial files (400-line contract limit), so the
                // Render body is cut out by brace matching instead of "up to the next method name":
                // the next method now lives in BoilerPanelView.Controls.cs.
                string renderBody = MethodBody(ViewSource(), "public void Render(BoilerPanelData data)");
                Assert.False(renderBody.Contains("_collapse", StringComparison.Ordinal),
                    "Render must never reset or overwrite the collapse state");
            });
        }

        // ------------------------------------------------------------------ native classes

        private static void NativeClassTests(TestRunner t)
        {
            t.Run("The panel is composed from the vanilla entity-panel USS classes", () =>
            {
                string styles = StylesSource();
                string[] nativeClasses =
                {
                    "entity-sub-panel",
                    "bg-sub-box--green",
                    "entity-panel__text",
                    "entity-panel__sub-text",
                    "entity-panel__heading-text",
                    "text--bold",
                    "game-text-normal",
                    "game-text--red",
                    "text--yellow",
                    "entity-panel__button-wrapper",
                    "entity-fragment__button",
                    "entity-fragment__button--red",
                    "entity-fragment__button--green",
                    "game-toggle",
                    "entity-panel__toggle",
                    "progress-bar",
                    "progress-bar--green",
                    "game-dropdown",
                    "entity-panel__name-wrapper",
                    "entity-panel__row"
                };
                foreach (string name in nativeClasses)
                {
                    Assert.Contains(styles, "\"" + name + "\"", "native USS class " + name);
                }
            });

            t.Run("The panel sections use the native nine-slice card helper", () =>
            {
                string view = ViewSource();
                Assert.Contains(view, "BoilerPanelNativeStyles.CreateSection", "native nine-slice cards");
                Assert.Contains(view, "NineSliceVisualElement", "cards are NineSliceVisualElement");
                Assert.Contains(StylesSource(), "new NineSliceVisualElement()", "nine-slice card factory");
            });
        }

        // ------------------------------------------------------------------ native controls

        private static void NativeControlTests(TestRunner t)
        {
            t.Run("Native buttons are taken from the vanilla DemolishableFragment template", () =>
            {
                string styles = StylesSource();
                Assert.Contains(styles, "Game/EntityPanel/DemolishableFragment", "vanilla button template");
                Assert.Contains(styles, "entity-fragment__button", "native fragment button class");
                Assert.Contains(styles, "LoadVisualElement", "VisualElementLoader usage");
                Assert.Contains(styles, "new Button()", "plain-button fallback stays available");
                Assert.Contains(ViewSource(), "BoilerPanelNativeStyles.CreateFragmentButton",
                    "the view creates native buttons through the factory");
            });

            t.Run("The condition bar is the native Timberborn ProgressBar with a label inside", () =>
            {
                string styles = StylesSource();
                Assert.Contains(styles, "Timberborn.CoreUI.ProgressBar", "native ProgressBar type");
                Assert.Contains(styles, "SetProgress(", "native SetProgress call");
                Assert.Contains(styles, "CreateProgressBar", "native bar factory");
                Assert.Contains(ViewSource(), "BoilerPanelNativeStyles.SetProgress", "the view updates the bar");
            });

            t.Run("The auto-maintenance control is a native game toggle", () =>
            {
                string styles = StylesSource();
                Assert.Contains(styles, "new Toggle(", "native Toggle creation");
                Assert.Contains(styles, "game-toggle", "native toggle class");
                Assert.Contains(ViewSource(), "BoilerPanelNativeStyles.CreateToggle", "the view uses the factory");
            });

            t.Run("The water-supply mode uses the game Dropdown with a guarded DropdownField fallback", () =>
            {
                string view = ViewSource();
                Assert.Contains(view, "new Dropdown()", "the native Dropdown is the main path");
                Assert.Contains(view, "Initialize(", "the native Dropdown is initialized with the list drawer");
                Assert.Contains(view, "DropdownItemsSetter", "the game items setter fills the dropdown");
                Assert.Contains(view, "IDropdownProvider", "the view implements the game provider interface");
                Assert.Contains(view, "UpdateSelectedValue", "programmatic refresh cannot fire the command");
                Assert.Contains(view, "new DropdownField", "a fallback field still exists");

                int nativeCall = view.IndexOf("new Dropdown()", StringComparison.Ordinal);
                int fallbackCall = view.IndexOf("new DropdownField", StringComparison.Ordinal);
                int caught = view.IndexOf("catch (Exception)", StringComparison.Ordinal);
                Assert.True(nativeCall >= 0 && fallbackCall > nativeCall,
                    "the native dropdown is built before the fallback");
                Assert.True(caught >= 0 && caught < fallbackCall,
                    "the fallback is only reached from the construction try/catch");
            });

            t.Run("Hiding the SEE block cannot close another panel's dropdown", () =>
            {
                string body = MethodBody(ViewSource(), "public void SetVisible(bool visible)");
                int guard = body.IndexOf("_visible && !visible", StringComparison.Ordinal);
                int hide = body.IndexOf("_waterMode?.Hide()", StringComparison.Ordinal);
                Assert.True(guard >= 0,
                    "the shared drawer is only hidden on a visible -> hidden transition");
                Assert.True(hide > guard, "the transition guard precedes the Hide() call");
            });

            t.Run("The fragment injects the game dropdown services", () =>
            {
                string fragment = FragmentSource();
                Assert.Contains(fragment, "VisualElementLoader", "native button template service");
                Assert.Contains(fragment, "DropdownListDrawer", "native dropdown list drawer");
                Assert.Contains(fragment, "DropdownItemsSetter", "native dropdown items setter");
            });
        }

        // ------------------------------------------------------------------ inline styles

        private static void InlineStyleTests(TestRunner t)
        {
            t.Run("Only the A1 status swatch sets an inline colour", () =>
            {
                int colourAssignments = 0;
                string owner = string.Empty;
                foreach (string file in UiSourceFiles())
                {
                    string text = File.ReadAllText(file);
                    int count = CountOccurrences(text, "style.backgroundColor =");
                    if (count > 0)
                    {
                        colourAssignments += count;
                        owner = Path.GetFileName(file);
                    }
                }

                Assert.Equal(1, colourAssignments, "exactly one inline background colour (the A1 swatch)");
                Assert.Equal("BoilerPanelNativeStyles.cs", owner,
                    "the inline swatch colour lives in the native styles helper");
            });

            t.Run("The panel never sets inline text colour or font size", () =>
            {
                foreach (string file in UiSourceFiles())
                {
                    string text = File.ReadAllText(file);
                    Assert.False(text.Contains("style.color =", StringComparison.Ordinal),
                        "no inline text colour in " + file);
                    Assert.False(text.Contains("fontSize", StringComparison.Ordinal),
                        "no inline font size in " + file);
                    Assert.False(text.Contains("style.unityFontStyleAndWeight", StringComparison.Ordinal),
                        "no inline font weight in " + file);
                }
            });

            t.Run("The view delegates every visual to the native styles helper", () =>
            {
                string view = ViewSource();
                Assert.False(view.Contains(".style.", StringComparison.Ordinal),
                    "BoilerPanelView must not touch inline styles at all");
            });
        }

        // ------------------------------------------------------------------ helpers

        private static string ViewSource() => ReadPanels("BoilerPanelView*.cs");

        private static string StylesSource() => ReadPanels("BoilerPanelNativeStyles*.cs");

        /// <summary>
        /// Concatenates every partial part of a panel source file. The vanilla-styled panel is split
        /// across <c>*.Controls.cs</c> parts to honour the contract 400-line limit (§11.5), so the
        /// source scans must look at the whole set instead of one file.
        /// </summary>
        /// <param name="pattern">File pattern inside <c>src/SEE.UI/Panels</c>.</param>
        /// <returns>The concatenated source text of all matching files, in ordinal file-name order.</returns>
        private static string ReadPanels(string pattern)
        {
            string folder = TestPaths.FromMod("src", "SEE.UI", "Panels");
            string[] files = Directory.GetFiles(folder, pattern);
            Array.Sort(files, StringComparer.Ordinal);
            var builder = new System.Text.StringBuilder();
            foreach (string file in files)
            {
                builder.AppendLine(File.ReadAllText(file));
            }

            return builder.ToString();
        }

        private static string FragmentSource() =>
            File.ReadAllText(TestPaths.FromMod("src", "SEE.UI", "Panels", "BoilerPanelFragment.cs"));

        /// <summary>
        /// Returns one method body (including its braces) by matching the opening brace at depth 0.
        /// The method text is scanned as source text only; the panel sources contain no brace
        /// literals inside strings or comments, so plain brace counting is exact here.
        /// </summary>
        /// <param name="source">Concatenated source text of the panel parts.</param>
        /// <param name="signature">Exact method signature prefix, for example <c>public void Render(</c>.</param>
        /// <returns>The substring from the opening brace to its matching closing brace.</returns>
        private static string MethodBody(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            Assert.True(start >= 0, signature + " exists");
            int open = source.IndexOf('{', start);
            Assert.True(open > start, signature + " has a body");
            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{')
                {
                    depth++;
                }
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(open, i - open + 1);
                    }
                }
            }

            Assert.True(false, signature + " body is closed");
            return string.Empty;
        }

        /// <summary>All production C# sources of SEE.UI (build output excluded).</summary>
        private static string[] UiSourceFiles()
        {
            string root = TestPaths.FromMod("src", "SEE.UI");
            var files = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal)
                    || file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                files.Add(file);
            }

            return files.ToArray();
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}
