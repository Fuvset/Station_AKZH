using System.Collections.Generic;

public class SignalData
{
    
    public Dictionary<(string, string), int> SegOccTrain = new()
    {
        [("Ч3beforeM7", "Ч3")] = 1,
        [("Ч3beforeM7", "Ч3M7mid")] = 1,
        [("beforeM7", "Ч3M7mid")] = 1,
        [("beforeM7", "M7")] = 1,
        [("Ч1M1first", "Ч1")] = 1,
        [("Ч1M1second", "Ч1M1first")] = 1,
        [("Ч1M1second", "Ч1M1mid")] = 1,
        [("beforeM1", "Ч1M1mid")] = 1,
        [("beforeM1", "M1")] = 1,
        [("M1", "H")] = 1,
        [("H", "1")] = 1,
        [("Ч2M5mid", "Ч2")] = 1,
        [("Ч2M5mid", "Ч2M5third")] = 1,
        [("beforeM5", "Ч2M5third")] = 1,
        [("beforeM5", "M5")] = 1,
        [("M5", "M5M3mid")] = 1,
        [("M5M3mid", "M5M3third")] = 1,
        [("M3", "M5M3third")] = 1,
        [("M7", "pastM7")] = 1,
        [("M3", "6")] = 1,

        [("H4", "Ч4")] = 1,
        [("H2", "Ч2")] = 1,
        [("H1", "Ч1")] = 1,
        [("H3", "Ч3")] = 1,
        [("H5", "Ч5")] = 1,
        [("M10", "Turn12_16mid")] = 1,
        [("Turn12_16mid", "H3")] = 1,
        [("Turn8B_M8mid", "H1")] = 1,
        [("M6", "Turn_6_A")] = 1,
        [("Turn_6_B", "Turn_6_A")] = 1,
        [("Turn_6_B", "Turn_14_J")] = 1,
        [("Turn_8_B", "1_AK")] = 1,
        [("Turn_8_B", "M8")] = 1,
        [("Turn_14_J", "H2")] = 1,
    };
    public Dictionary<string, SignalState> SignalsState = new()
    {
        ["Ч1"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(),
                ["red"] = new LampState(),
                ["green"] = new LampState(on: true),
                ["yellow"] = new LampState(),
            },
            Manual = false,
        },
        ["Ч2"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(),
                ["red"] = new LampState(),
                ["green"] = new LampState(on: true),
                ["yellow"] = new LampState(),
            },
            Manual = false,
        },
        ["Ч3"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(),
                ["red"] = new LampState(),
                ["green"] = new LampState(on: true),
                ["yellow"] = new LampState(),
            },
            Manual = false,
        },
        ["Ч4"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(),
                ["red"] = new LampState(),
                ["green"] = new LampState(on: true),
                ["yellow"] = new LampState(),
            },
            Manual = false,
        },
        ["Ч5"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(),
                ["red"] = new LampState(),
                ["green"] = new LampState(on: true),
                ["yellow"] = new LampState(),
            },
            Manual = false,
        },
        ["M7"] = new SignalState
        {
            Lamps = new()
            {
                ["blue"] = new LampState(on: true),
                ["white"] = new LampState(),
            },
            Manual = false,
        },
        ["M1"] = new SignalState
        {
            Lamps = new()
            {
                ["blue"] = new LampState(on: true),
                ["white"] = new LampState(),
            },
            Manual = false,
        },
        ["M5"] = new SignalState
        {
            Lamps = new()
            {
                ["blue"] = new LampState(on: true),
                ["white"] = new LampState(),
            },
            Manual = false,
        },
        ["M3"] = new SignalState
        {
            Lamps = new()
            {
                ["blue"] = new LampState(on: true),
                ["white"] = new LampState(),
            },
            Manual = false,
        },
        ["1"] = new SignalState
        {
            Lamps = new()
            {
                ["yellow"] = new LampState(on: true),
                ["green"] = new LampState(),
                ["red"] = new LampState(),
            },
            Manual = false,
        },
        ["M8"] = new SignalState
        {
            Lamps = new()
            {
                ["white"] = new LampState(on: true),
                ["blue"] = new LampState(),
            },
            Manual = false,
        },
    };
}