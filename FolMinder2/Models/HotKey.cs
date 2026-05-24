using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace FolMinder2.Models
{
    public record HotKey(bool Alt, bool Control, bool Shift, bool Win, Key Key)
    {
        public static HotKey Default { get; } = new HotKey(Alt: true, Control: true, Shift: false, Win: false, Key.F);

        /// <summary>
        /// 修飾キーの組み合わせが有効かどうかを検証します。
        /// 少なくとも1つの修飾キーが必要です。
        /// </summary>
        public static bool Validate(bool alt, bool control, bool shift, bool win)
        {
            return alt || control || shift || win;
        }

        /// <summary>
        /// HotKeyの修飾キーの組み合わせが有効かどうかを検証します。
        /// 少なくとも1つの修飾キーが必要です。
        /// </summary>
        public static bool Validate(HotKey hotKey)
            => Validate(hotKey.Alt, hotKey.Control, hotKey.Shift, hotKey.Win);

        public override string ToString()
        {
            return $"HotKey(Alt: {Alt}, Control: {Control}, Shift: {Shift}, Win: {Win}, Key: {Key})";
        }
    }
}
