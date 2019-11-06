﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sonic3AIR_ModLoader
{
    public partial class KeyBindingDialog : Form
    {
        List<string> KeyBindings { get => GetKeys(); }
        string OriginalKeybinding = "";

        private List<string> GetKeys()
        {
            List<string> functionKeys = new List<string>();

            functionKeys.AddRange(new string[] {
            "Enter","Space","Backspace","Up","Down","Left","Right","A","B","C","D","E","F","G","H","I","J","K","L",
            "M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z","Comma","Period","Colon","Semicolon",
            "Quote","Slash","Backslash","Minus","Equals","BracketLeft","BracketRight","0","1","2","3","4","5","6","7","8","9",
            "Numpad0","Numpad1","Numpad2","Numpad3","Numpad4","Numpad5","Numpad6","Numpad7","Numpad8","Numpad9","NumpadPlus",
            "NumpadMinus","NumpadMultiply","NumpadDivide","NumpadPeriod","Insert","Delete","Home","End","PageUp","PageDown"
            });

            List<string> Keys = new List<string>();
            Keys.Add("");
            Keys.AddRange(functionKeys);
            return Keys;
        }

        private KeyBindingDialog Instance;

        public KeyBindingDialog()
        {
            InitializeComponent();
            Instance = this;
            UserLanguage.ApplyLanguage(ref Instance);
            keyBox.DataSource = KeyBindings;
            RadioButton1_CheckedChanged(null, null);
        }

        public string ShowInputDialog(string keybind)
        {
            OriginalKeybinding = keybind;
            resultText.Text = $"{keybind} {Program.LanguageResource.GetString("KeybindingsExistingNote")}"; ;
            resultText.Tag = keybind;
            if (KeyBindings.Contains(keybind)) keyBox.SelectedIndex = keyBox.Items.IndexOf(OriginalKeybinding);
            if (this.ShowDialog() == DialogResult.OK)
            {
                keybind = resultText.Tag.ToString();
            }
            OriginalKeybinding = "";
            return keybind;
        }

        private void FlowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (inputDeviceRadioButton1.Checked)
            {
                ToggleKeyboardBindingsArea(true);
                ToggleControllerBindingsArea(false);
                ToggleCustomBindingsArea(false);
            }
            else if (inputDeviceRadioButton3.Checked)
            {
                ToggleKeyboardBindingsArea(false);
                ToggleControllerBindingsArea(false);
                ToggleCustomBindingsArea(true);
            }


            void ToggleKeyboardBindingsArea(bool enabled)
            {
                keyBox.Enabled = enabled;
                keyLabel.Enabled = enabled;
                UpdateResultText(enabled);
            }

            void ToggleControllerBindingsArea(bool enabled)
            {
                UpdateControllerInputType(enabled);
                UpdateResultText(enabled);
            }

            void ToggleCustomBindingsArea(bool enabled)
            {
                resultLabel.Enabled = enabled;
                resultText.Enabled = enabled;
                UpdateResultText(!enabled);
            }
        }

        private void UpdateResultText(bool ShowExistingString = true)
        {
            if (keyBox.SelectedIndex != 0 && inputDeviceRadioButton1.Checked)
            {
                resultText.Text = keyBox.SelectedItem.ToString();
                resultText.Tag = keyBox.SelectedItem.ToString();
            }
            else
            {
                if (ShowExistingString)
                {
                    resultText.Text = $"{OriginalKeybinding} {Program.LanguageResource.GetString("KeybindingsExistingNote")}";
                }
                else
                {
                    resultText.Text = OriginalKeybinding;
                    resultText.Tag = OriginalKeybinding;
                }
            }
        }

        private void KeyBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private void UpdateControllerInputType(bool isSectionEnabled)
        {
            if (isSectionEnabled)
            {

            }
            else
            {
                ToggleButtonIDInput(false);
                ToggleAxisInput(false);


            }

            void ToggleButtonIDInput(bool enabled)
            {

            }

            void ToggleAxisInput(bool enabled)
            {
                UpdateJoyAxisUI(null);
            }

        }

        private void ControllerInputTypeRadio1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControllerInputType(true);
            UpdateResultText();
        }

        private void ButtonIDNUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private int AxisCurrentDirection = 0;
        private void Label5_Click(object sender, EventArgs e)
        {
            UpdateJoyAxisUI(sender);
        }

        private void UpdateJoyAxisUI(object sender)
        {
            UpdateAxisDiagram(AxisCurrentDirection);

            void UpdateAxisDiagram(int direction = 0)
            {
                AxisCurrentDirection = direction;
                if (direction == 0)
                {
                    Up(true);
                    Left(false);
                    Down(false);
                    Right(false);
                }
                else if (direction == 1)
                {
                    Up(false);
                    Left(false);
                    Down(false);
                    Right(true);
                }
                else if (direction == 2)
                {
                    Up(false);
                    Left(false);
                    Down(true);
                    Right(false);
                }
                else if (direction == 3)
                {
                    Up(false);
                    Left(true);
                    Down(false);
                    Right(false);
                }


                void Left(bool enabled)
                {

                }


                void Right(bool enabled)
                {

                }


                void Up(bool enabled)
                {

                }


                void Down(bool enabled)
                {

                }
            }
        }

        private void AxisTypeRadio5_CheckedChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private void AxisRightRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private void AxisIDNUD_ValueChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private void AxisCustomStringBox_TextChanged(object sender, EventArgs e)
        {
            UpdateResultText();
        }

        private void resultText_TextChanged(object sender, EventArgs e)
        {
            if (inputDeviceRadioButton3.Checked == true)
            {
                resultText.Tag = resultText.Text;
            }
        }

        private void getInputButton_Click(object sender, EventArgs e)
        {
            
            JoystickReaderDialog dlg = new JoystickReaderDialog();
            if (dlg.ShowInputDialog() == DialogResult.OK)
            {
                inputDeviceRadioButton3.Checked = true;
                resultText.Text = dlg.Result;
            }

            



        }
    }
}