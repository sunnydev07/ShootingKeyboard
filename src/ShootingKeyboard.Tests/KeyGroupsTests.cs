using System.Linq;
using ShootingKeyboard.Models;
using Xunit;

namespace ShootingKeyboard.Tests;

public class KeyGroupsTests
{
    [Fact]
    public void KeyGroups_All_ContainsAllElevenGroups()
    {
        var allGroups = KeyGroups.All;

        Assert.Equal(11, allGroups.Count);
        Assert.Contains(KeyGroups.Letters, allGroups);
        Assert.Contains(KeyGroups.Numbers, allGroups);
        Assert.Contains(KeyGroups.WASD, allGroups);
        Assert.Contains(KeyGroups.Arrows, allGroups);
        Assert.Contains(KeyGroups.FKeys, allGroups);
        Assert.Contains(KeyGroups.Space, allGroups);
        Assert.Contains(KeyGroups.Enter, allGroups);
        Assert.Contains(KeyGroups.Modifiers, allGroups);
        Assert.Contains(KeyGroups.Punctuation, allGroups);
        Assert.Contains(KeyGroups.Navigation, allGroups);
        Assert.Contains(KeyGroups.Numpad, allGroups);
    }

    [Theory]
    [InlineData(0x57)] // W
    [InlineData(0x41)] // A
    [InlineData(0x53)] // S
    [InlineData(0x44)] // D
    public void GetGroupForKey_WASDKeys_ReturnsWASDGroup(int keyCode)
    {
        var group = KeyGroups.GetGroupForKey(keyCode);
        Assert.Equal(KeyGroups.WASD, group);
    }

    [Fact]
    public void GetGroupForKey_AllNonWASDLetters_ReturnsLettersGroup()
    {
        for (int vk = 0x41; vk <= 0x5A; vk++)
        {
            if (vk is 0x57 or 0x41 or 0x53 or 0x44)
                continue; // Skip W, A, S, D

            var group = KeyGroups.GetGroupForKey(vk);
            Assert.Equal(KeyGroups.Letters, group);
        }
    }

    [Fact]
    public void GetGroupForKey_NumberKeys0Through9_ReturnsNumbersGroup()
    {
        for (int vk = 0x30; vk <= 0x39; vk++)
        {
            var group = KeyGroups.GetGroupForKey(vk);
            Assert.Equal(KeyGroups.Numbers, group);
        }
    }

    [Fact]
    public void GetGroupForKey_FKeysF1ThroughF24_ReturnsFKeysGroup()
    {
        for (int vk = 0x70; vk <= 0x87; vk++)
        {
            var group = KeyGroups.GetGroupForKey(vk);
            Assert.Equal(KeyGroups.FKeys, group);
        }
    }

    [Theory]
    [InlineData(0x25)] // Left
    [InlineData(0x26)] // Up
    [InlineData(0x27)] // Right
    [InlineData(0x28)] // Down
    public void GetGroupForKey_ArrowKeys_ReturnsArrowsGroup(int keyCode)
    {
        var group = KeyGroups.GetGroupForKey(keyCode);
        Assert.Equal(KeyGroups.Arrows, group);
    }

    [Fact]
    public void GetGroupForKey_SpaceKey_ReturnsSpaceGroup()
    {
        var group = KeyGroups.GetGroupForKey(0x20);
        Assert.Equal(KeyGroups.Space, group);
    }

    [Fact]
    public void GetGroupForKey_EnterKey_ReturnsEnterGroup()
    {
        var group = KeyGroups.GetGroupForKey(0x0D);
        Assert.Equal(KeyGroups.Enter, group);
    }

    [Theory]
    [InlineData(0x10)] // VK_SHIFT
    [InlineData(0x11)] // VK_CONTROL
    [InlineData(0x12)] // VK_MENU / Alt
    [InlineData(0x5B)] // VK_LWIN
    [InlineData(0x5C)] // VK_RWIN
    [InlineData(0xA0)] // VK_LSHIFT
    [InlineData(0xA1)] // VK_RSHIFT
    [InlineData(0xA2)] // VK_LCONTROL
    [InlineData(0xA3)] // VK_RCONTROL
    [InlineData(0xA4)] // VK_LMENU
    [InlineData(0xA5)] // VK_RMENU
    public void GetGroupForKey_ModifierKeys_ReturnsModifiersGroup(int keyCode)
    {
        var group = KeyGroups.GetGroupForKey(keyCode);
        Assert.Equal(KeyGroups.Modifiers, group);
    }

    [Theory]
    [InlineData(0x21)] // VK_PRIOR / PageUp
    [InlineData(0x22)] // VK_NEXT / PageDown
    [InlineData(0x23)] // VK_END
    [InlineData(0x24)] // VK_HOME
    [InlineData(0x2D)] // VK_INSERT
    [InlineData(0x2E)] // VK_DELETE
    public void GetGroupForKey_NavigationKeys_ReturnsNavigationGroup(int keyCode)
    {
        var group = KeyGroups.GetGroupForKey(keyCode);
        Assert.Equal(KeyGroups.Navigation, group);
    }

    [Fact]
    public void GetGroupForKey_NumpadKeys_ReturnsNumpadGroup()
    {
        for (int vk = 0x60; vk <= 0x6F; vk++)
        {
            var group = KeyGroups.GetGroupForKey(vk);
            Assert.Equal(KeyGroups.Numpad, group);
        }
    }

    [Theory]
    [InlineData(0xBA)] // Semicolon
    [InlineData(0xBB)] // Plus
    [InlineData(0xBC)] // Comma
    [InlineData(0xBD)] // Minus
    [InlineData(0xBE)] // Period
    [InlineData(0xBF)] // Slash
    [InlineData(0xC0)] // Tilde
    [InlineData(0xDB)] // OpenBracket
    [InlineData(0xDC)] // Backslash
    [InlineData(0xDD)] // CloseBracket
    [InlineData(0xDE)] // Quote
    public void GetGroupForKey_PunctuationKeys_ReturnsPunctuationGroup(int keyCode)
    {
        var group = KeyGroups.GetGroupForKey(keyCode);
        Assert.Equal(KeyGroups.Punctuation, group);
    }
}
