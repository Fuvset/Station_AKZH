using System;
using System.Linq;
using System.Collections.Generic;

public class SignalManager
{
    public SignalData Data;

    public Station Station;

    public SignalManager(SignalData data, Station station)
    {
        Data = data;
        Station = station;
    }
    public void make_signal_state(string name, IEnumerable<string> colors)
    {
        var signal = new SignalState();
        foreach (var color in colors)
            signal.Lamps[color] = new LampState();

        Data.SignalsState[name] = signal;
    }


    public void calculate_enter_signal(string name)
    {
        if (get_switch_state_num(15) == "-")
        {
            enable_two_yellow_train(name);
        }
        if (get_switch_state_num(7) == "+" && get_switch_state_num(15) == "-")
        {
            enable_two_yellow_train(name);
        }
        if (get_switch_state_num(7) == "+" && get_switch_state_num(15) == "+")
        {
            enable_one_yellow_train(name);
        }
        if (get_switch_state_num(7) == "-" && get_switch_state_num(15) == "+")
        {
            enable_two_yellow_train(name);
        }
    }

    public void return_to_red_after_finishing(Route r)
    {
        if (r == null) return;

        var cfg = RouteSignalMap.Map.GetValueOrDefault((r.From, r.To));
        if (cfg == null) return;

        foreach (var name in cfg.Keys)
        {
            if (is_signal_used_by_other_routes(name, r))
                continue;

            foreach (var kv in Data.SignalsState[name].Lamps.ToList())
            {
                var lampName = kv.Key;
                var lamp = kv.Value;
                if (lampName == "white" && name == "H")
                    continue;

                set_signal(name, lampName, false);
                lamp.Blink = false;
            }

            if (if_color_in_lamp(name, "red"))
                set_signal(name, "red", true);
            else if (if_color_in_lamp(name, "blue"))
                set_signal(name, "blue", true);
            else
                foreach (var color in get_lamp_colors(name).Keys)
                    set_signal(name, color, false);
        }
    }

    public void invite_signal_on_off(string signalName)
    {
        if (get_lamp_state(signalName, "white"))
            set_signal(signalName, "white", false);
        else
            set_signal(signalName, "white", true);
    }
    //Пока тестовая трёхблочная блокировка
    public void after_train_passed_seg((string, string) seg, (string, string) rev)
    {
        var signals = SegmentConfig.SegmentToSignal.GetValueOrDefault(seg)
                      ?? SegmentConfig.SegmentToSignal.GetValueOrDefault(rev, new List<string>());

        if (seg == ("M3", "6") || seg == ("6", "M3"))
        {
            foreach (var (route_id, route_data) in route_manager.get_active_routes_items())
            {
                if (route_data["end"] == "1" || route_data["start"] == "1")
                    signals = new List<string>();
            }
        }

        if (seg == ("H", "M1") || seg == ("M1", "H"))
        {
            foreach (var (route_id, route_data) in route_manager.get_active_routes_items())
            {
                if (route_data["end"] == "6" || route_data["start"] == "6")
                    signals = new List<string>();
            }
        }

        foreach (var signal_name in signals)
        {
            if (!has_signal_in_states(signal_name)) continue;

            foreach (var color in get_lamp_colors(signal_name).Keys)
            {
                if (color == "red" || color == "blue")
                    set_signal(signal_name, color, true);
                else
                    set_signal(signal_name, color, false);
            }
        }
    }

    public void set_signals_to_route(int rid)
    {
        var data = route_manager.get_active_routes(rid);
        if (data == null) return;

        var a = data.GetValueOrDefault("start");
        var b = data.GetValueOrDefault("end");
        var key = (a, b);

        var cfg = RouteConfig.RouteSignalMap.GetValueOrDefault(key);
        if (cfg == null) return;

        foreach (var name in cfg.Keys)
        {
            if (name == "H")
            {
                calculate_enter_signal(name);
            }
            else
            {
                foreach (var (color, lamp_cfg) in cfg[name].Lamps)
                {
                    set_signal(name, color, lamp_cfg.On);

                    foreach (var color_in_state in get_lamp_colors(name).Keys)
                    {
                        if (color_in_state == "white" && name == "Ч") continue;
                        if (cfg[name].Lamps.ContainsKey(color_in_state)) continue;
                        set_signal(name, color_in_state, false);
                    }
                }
            }
        }
    }
    public void set_signal(string name, string color, bool onStatus)
    {
        Data.SignalsState[name].Lamps[color].On = onStatus;
    }
    public bool get_lamp_state(string name, string color)
    {
        return Data.SignalsState[name].Lamps[color].On;
    }
    public Dictionary<string, LampState> get_lamp_colors(string name)
    {
        return Data.SignalsState[name].Lamps;
    }
    public bool has_signal_in_states(string name)
    {
        return Data.SignalsState.ContainsKey(name);
    }
    public bool if_color_in_lamp(string name, string color)
    {
        return Data.SignalsState[name].Lamps.ContainsKey(color);
    }
    public void clear_signal(string name)
    {
        if (!has_signal_in_states(name))
            return;

        foreach (var color in Data.SignalsState[name].Lamps.Keys)
        {
            Data.SignalsState[name].Lamps[color].On = false;
            Data.SignalsState[name].Lamps[color].Blink = false;
        }
    }
    public void set_all_to_red()
    {
        var color = "red";
        var bannedSignals = new List<string> { "ALB_Sect2", "ALB_Sect1-2" };

        foreach (var name in Data.SignalsState.Keys)
        {
            if (bannedSignals.Contains(name))
                continue;

            if (if_color_in_lamp(name, color))
                set_signal(name, color, true);
            else if (if_color_in_lamp(name, "blue"))
                set_signal(name, "blue", true);
        }
    }
    public void set_signal_red(string name)
    {
        clear_signal(name);

        if (if_color_in_lamp(name, "red"))
            set_signal(name, "red", true);
        else
            set_signal(name, "blue", true);
    }
    public void set_signal_green(string name)
    {
        clear_signal(name);
        set_signal(name, "green", true);
    }
    public void set_signal_white(string name)
    {
        clear_signal(name);
        set_signal(name, "white", true);
    }
    public void set_blink(string name, string color, bool blinkstatus)
    {
        Data.SignalsState[name].Lamps[color].Blink = blinkstatus;
    }
    public void enable_two_yellow_train(string name)
    {
        var names = new List<string> { "Ч1", "Ч2", "Ч3", "Ч4", "Ч5", "H" };

        if (names.Contains(name))
        {
            set_signal(name, "yellow", true);
            set_signal(name, "yellow1", true);

            foreach (var color in get_lamp_colors(name).Keys)
            {
                if (color == "yellow" || color == "yellow1")
                    continue;
                if (color == "white" && names.Contains(name))
                    continue;

                set_signal(name, color, false);
            }
        }
        else
        {
            set_signal(name, "yellow", true);

            foreach (var color in get_lamp_colors(name).Keys)
            {
                if (color == "yellow")
                    continue;
                if (color == "white" && name == "Ч")
                    continue;

                set_signal(name, color, false);
            }
        }
    }

    public void enable_one_yellow_train(string name)
    {
        set_signal(name, "yellow", true);

        foreach (var color in get_lamp_colors(name).Keys)
        {
            if (color == "yellow")
                continue;
            if (color == "white" && name == "Ч")
                continue;

            set_signal(name, color, false);
        }
    }

    public void enable_red_train(string name)
    {
        set_signal(name, "red", true);

        foreach (var color in get_lamp_colors(name).Keys)
        {
            if (color == "red")
                continue;
            if (color == "white" && name == "Ч")
                continue;

            set_signal(name, color, false);
        }
    }

    public void enable_green_train(string name)
    {
        set_signal(name, "green", true);

        foreach (var color in get_lamp_colors(name).Keys)
        {
            if (color == "green")
                continue;
            if (color == "white" && name == "Ч")
                continue;

            set_signal(name, color, false);
        }
    }
    public string get_switch_state_num(int switchNumber)
    {
        if (!Station.Switches.ContainsKey(switchNumber))
            return "None";

        return Station.Switches[switchNumber].Plus ? "+" : "-";
    }
    public bool is_segment_occupied(string seg)
    {
        if (seg == null)
            return false;

        return Station.Occupied.Contains(seg);
    }

    public bool is_route_steps_free(IEnumerable<string> segs)
    {
        foreach (var seg in segs)
        {
            if (is_segment_occupied(seg))
                return false;
        }

        return true;
    }
    public string get_first_segment_from_route_steps(string[] segs)
    {
        return segs.Length > 0 ? segs[0] : null;
    }
    public void on_route_established(Route r)
    {
        if (r.Shunting)
        {
            set_signal_white(r.From);
        }
        else if (HasSwitchesAfter(r.From))  // пока заглушка, нужна карта "сигнал → стрелки за ним"
        {
            calculate_enter_signal(r.From);
        }
        else
        {
            set_signal_green(r.From);
        }
    }
    //Флаг_фазы_мигания
    bool signal_blink_phase = false;
}

