using System;
using System.Collections.Generic;
using Ares.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Modal command input; preserves the underlying view and freezes gameplay while typing.</summary>
    internal sealed class MissionCommandConsole : IDisposable
    {
        readonly VisualElement root;
        readonly Func<Campaign> campaign;
        readonly Func<bool> tutorial;
        readonly List<VisualElement> disabled = new List<VisualElement>();
        VisualElement shade, previousFocus;
        TextField input;
        Label feedback;
        Keyboard keyboard;
        bool toggleRequested;
        int lastExecuteFrame = -1;
        public bool IsOpen => shade != null;
        public MissionCommandConsole(VisualElement root, Func<Campaign> campaign, Func<bool> tutorial)
        { this.root = root; this.campaign = campaign; this.tutorial = tutorial; Bind(Keyboard.current); }
        void Bind(Keyboard device)
        {
            if (keyboard == device) return;
            if (keyboard != null) keyboard.onTextInput -= TextInput;
            keyboard = device;
            if (keyboard != null) keyboard.onTextInput += TextInput;
        }
        void TextInput(char character)
        {
            if (character != '\'') return;
            var focused = root.panel?.focusController.focusedElement as VisualElement;
            // Apostrophes in names/other text fields remain ordinary typing.
            if (!IsOpen && (focused is TextField || focused?.GetFirstAncestorOfType<TextField>() != null)) return;
            toggleRequested = true;
        }
        public bool HandleInput(bool canOpen)
        {
            Bind(Keyboard.current);
            bool wasOpen = IsOpen;
            if (toggleRequested)
            {
                toggleRequested = false;
                if (IsOpen) Close(); else if (canOpen) Open();
            }
            if (IsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
            if (IsOpen && keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) Execute();
            return wasOpen || IsOpen;
        }
        void Open()
        {
            previousFocus = root.panel?.focusController.focusedElement as VisualElement;
            foreach (var child in root.Children()) if (child.enabledSelf) { child.SetEnabled(false); disabled.Add(child); }
            shade = Box(root, "command-console-shade"); shade.name = "command-console";
            var panel = Box(shade, "command-console-panel");
            var header = Box(panel, "row command-console-header");
            Text(header, "CONSOLE DE COMANDOS", "heading");
            Button(header, "×", Close, "ghost").tooltip = PresentationLocalizer.Text("Fechar console");
            Text(panel, "Digite /event (WindStorm) para iniciar uma tempestade de areia.", "small");
            input = new TextField { name = "command-input", maxLength = 128, multiline = false };
            panel.Add(input);
            input.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) { Execute(); evt.StopImmediatePropagation(); }
                else if (evt.character == '\'') evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
            input.RegisterValueChangedCallback(evt => { if (evt.newValue == "'") input.SetValueWithoutNotify(""); });
            var actions = Box(panel, "row");
            Button(actions, "Executar comando", Execute).name = "command-execute";
            Button(actions, "Fechar console", Close, "ghost");
            feedback = Text(panel, "O jogo fica pausado enquanto você digita. Enter executa; Esc ou ' fecha.", "command-feedback");
            feedback.name = "command-feedback";
            input.schedule.Execute(() => input?.Focus());
            MissionVisualStyle.Open(panel);
        }
        void Execute()
        {
            // UI Toolkit submit and Input System can report the same Enter in one frame.
            if (!IsOpen || input == null || lastExecuteFrame == Time.frameCount) return;
            lastExecuteFrame = Time.frameCount;
            var result = tutorial() ? MissionCommandResult.Unavailable : MissionCommands.Execute(campaign(), input.value);
            string message = result == MissionCommandResult.StormStarted ? "Tempestade iniciada! Feche o console para continuar."
                : result == MissionCommandResult.Unavailable ? "Use este comando durante a exploração, após o tutorial e a divisão de energia."
                : result == MissionCommandResult.EventActive ? "Já existe um evento em andamento. Aguarde o fim da tempestade atual."
                : result == MissionCommandResult.UnknownCommand ? "Comando desconhecido. Use /help para ver os comandos."
                : "Comandos: /event (WindStorm) ou /event WindStorm — iniciar tempestade de areia.";
            feedback.text = PresentationLocalizer.Text(message);
            feedback.EnableInClassList("success", result == MissionCommandResult.StormStarted);
            input.SelectAll(); input.Focus();
        }
        public void Close()
        {
            shade?.RemoveFromHierarchy(); shade = null; input = null; feedback = null;
            foreach (var child in disabled) child.SetEnabled(true); disabled.Clear();
            previousFocus?.Focus(); previousFocus = null;
        }
        public void Dispose() { Close(); if (keyboard != null) keyboard.onTextInput -= TextInput; keyboard = null; }
    }
}
