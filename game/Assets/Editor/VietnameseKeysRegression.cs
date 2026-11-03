using BaGio12.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class VietnameseKeysRegression
{
    static readonly (string before, string after, bool numbers, Key expected)[] Cases =
    {
        ("a", "ă", false, Key.W), ("ă", "aw", false, Key.W), ("", "ư", false, Key.W), ("o", "ơ", false, Key.W),
        ("a", "á", false, Key.S), ("á", "as", false, Key.S), ("ă", "ắ", false, Key.S), ("hoa", "hóa", false, Key.S),
        ("a", "à", false, Key.F), ("a", "ả", false, Key.R), ("a", "ã", false, Key.X), ("a", "ạ", false, Key.J),
        ("d", "đ", false, Key.D), ("đ", "dd", false, Key.D), ("a", "â", false, Key.A), ("â", "aa", false, Key.A),
        ("e", "ê", false, Key.E), ("o", "ô", false, Key.O),
        ("a", "á", true, Key.Digit1), ("a", "à", true, Key.Digit2), ("a", "â", true, Key.Digit6),
        ("o", "ơ", true, Key.Digit7), ("a", "ă", true, Key.Digit8), ("d", "đ", true, Key.Digit9),
        ("a", "a", false, Key.None),
    };

    public static void Run()
    {
        int failed = 0;
        foreach (var c in Cases)
        {
            var actual = VietnameseKeys.Trigger(c.before, c.after, c.numbers);
            if (actual == c.expected) continue;
            failed++;
            Debug.LogError($"VietnameseKeys {c.before} -> {c.after} numbers={c.numbers}: expected {c.expected}, got {actual}");
        }
        Debug.Log($"VIETNAMESE_KEYS_REGRESSION {(failed == 0 ? "PASS" : "FAIL")} {Cases.Length - failed}/{Cases.Length}");
        EditorApplication.Exit(failed == 0 ? 0 : 1);
    }
}
