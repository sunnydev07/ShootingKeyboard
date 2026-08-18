using System;
using System.Collections.Generic;
using System.Threading;
using ShootingKeyboard.Models;
using ShootingKeyboard.Services;
using Xunit;

namespace ShootingKeyboard.Tests;

public class KeyboardHookTests
{
    [Fact]
    public void KeyEvent_Properties_AreAssignedCorrectly()
    {
        // Arrange & Act
        var keyEvent = new KeyEvent(
            keyCode: 0x41, // 'A'
            isPressed: true,
            isExtended: false,
            scanCode: 0x1E,
            timestamp: 123456,
            isInjected: false);

        // Assert
        Assert.Equal(0x41, keyEvent.KeyCode);
        Assert.True(keyEvent.IsPressed);
        Assert.False(keyEvent.IsExtended);
        Assert.Equal(0x1E, keyEvent.ScanCode);
        Assert.Equal(123456u, keyEvent.Timestamp);
        Assert.False(keyEvent.IsInjected);
    }

    [Fact]
    public void KeyPressedEventArgs_Properties_DelegateToKeyEvent()
    {
        // Arrange
        var keyEvent = new KeyEvent(
            keyCode: 0x20, // Space
            isPressed: false,
            isExtended: true,
            scanCode: 0x39,
            timestamp: 9999,
            isInjected: true);

        // Act
        var eventArgs = new KeyPressedEventArgs(keyEvent);

        // Assert
        Assert.Same(keyEvent, eventArgs.KeyEvent);
        Assert.Equal(0x20, eventArgs.KeyCode);
        Assert.False(eventArgs.IsPressed);
        Assert.True(eventArgs.IsExtended);
        Assert.Equal(0x39, eventArgs.ScanCode);
        Assert.Equal(9999u, eventArgs.Timestamp);
        Assert.True(eventArgs.IsInjected);
    }

    [Fact]
    public void KeyboardHookService_InitialState_IsNotRunning()
    {
        // Arrange & Act
        using var service = new KeyboardHookService();

        // Assert
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void KeyboardHookService_SimulateKeyEvent_TriggersKeyPressedEvent()
    {
        // Arrange
        using var service = new KeyboardHookService();
        using var eventFired = new ManualResetEventSlim(false);
        KeyPressedEventArgs? receivedArgs = null;

        service.KeyPressed += (sender, args) =>
        {
            receivedArgs = args;
            eventFired.Set();
        };

        var keyEvent = new KeyEvent(0x41, true);

        // Act
        service.SimulateKeyEvent(keyEvent);
        var signaled = eventFired.Wait(TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(signaled, "KeyPressed event was not received within timeout.");
        Assert.NotNull(receivedArgs);
        Assert.Equal(0x41, receivedArgs.KeyCode);
        Assert.True(receivedArgs.IsPressed);
    }

    [Fact]
    public void KeyboardHookService_RapidTypingSimulation_HandlesAllEventsInOrder()
    {
        // Arrange
        using var service = new KeyboardHookService();
        var receivedKeys = new List<int>();
        var countdown = new CountdownEvent(5);

        service.KeyPressed += (sender, args) =>
        {
            lock (receivedKeys)
            {
                receivedKeys.Add(args.KeyCode);
            }
            countdown.Signal();
        };

        int[] expectedSequence = { 0x57, 0x41, 0x53, 0x44, 0x20 }; // W, A, S, D, Space

        // Act
        foreach (var vk in expectedSequence)
        {
            service.SimulateKeyEvent(new KeyEvent(vk, true));
        }

        var completed = countdown.Wait(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(completed, "Countdown did not complete in time.");
        lock (receivedKeys)
        {
            Assert.Equal(5, receivedKeys.Count);
            Assert.Equal(expectedSequence, receivedKeys);
        }
    }

    [Fact]
    public void KeyboardHookService_MultipleStopCalls_AreSafe()
    {
        // Arrange
        using var service = new KeyboardHookService();

        // Act & Assert (should not throw)
        service.Stop();
        service.Stop();
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void KeyboardHookService_StartAndStop_TogglesIsRunning()
    {
        // Arrange
        using var service = new KeyboardHookService();

        // Act - Start
        service.Start();
        var wasRunning = service.IsRunning;

        // Act - Stop
        service.Stop();
        var isRunningAfterStop = service.IsRunning;

        // Assert
        Assert.True(wasRunning);
        Assert.False(isRunningAfterStop);
    }

    [Fact]
    public void KeyboardHookService_MultipleStartCalls_AreIdempotent()
    {
        // Arrange
        using var service = new KeyboardHookService();

        // Act
        service.Start();
        service.Start();

        // Assert
        Assert.True(service.IsRunning);

        service.Stop();
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void KeyboardHookService_Dispose_StopsHookCleanly()
    {
        // Arrange
        var service = new KeyboardHookService();
        service.Start();
        Assert.True(service.IsRunning);

        // Act
        service.Dispose();

        // Assert
        Assert.False(service.IsRunning);
    }
}
