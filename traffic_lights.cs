using System.Linq;
public void calculate_train_signal(string name)
{
    switch (name)
    {
        case "H1":
            if (seg_occ_train[("M2H1_mid", "M2H1_third")] == 0){
                enable_red_train(name);
                Console.WriteLine(name);
            }
            if (get_switch_state_num("ALB_Turn4-6") == "+" && get_switch_state_num("ALB_Turn2") == "+"){
                enable_green_train(name);
                Console.WriteLine(name);
            }
            break;

        case "H2":
            if (get_switch_state_num("ALB_Turn4-6") == "-"){
                enable_two_yellow_train(name);
                Console.WriteLine(name);
            }
            if (seg_occ_train[("M6H2", "M6")] == 0){
                enable_red_train(name);
                Console.WriteLine(name);
            }
            if (get_switch_state_num("ALB_Turn4-6") == "+"){
                enable_green_train(name);
                Console.WriteLine(name);
            }
            break;

        case "H3":
            if (get_switch_state_num("ALB_Turn2") == "-"){
                enable_two_yellow_train(name);
                Console.WriteLine(name);
            }
            if (seg_occ_train[("M2", "M2H1_mid")] == 0){
                enable_red_train(name);
            }
            break;

        case "H4":
            if (get_switch_state_num("ALB_Turn8") == "-"){
                Console.WriteLine(name);
                enable_two_yellow_train(name);
            }
            if (seg_occ_train[("M6H2", "M6")] == 0){
                enable_red_train(name);
            }
            break;
    }
}

public void calculate_enter_signal(string name)
{
    if (get_switch_state_num("AKZHT_Turn13-15") == "-"){
        enable_two_yellow_train(name);
    }
    if (get_switch_state_num("AKZHT_Turn5-7") == "+" && get_switch_state_num("AKZHT_Turn13-15") == "-"){
        enable_two_yellow_train(name);
    }
    if (get_switch_state_num("AKZHT_Turn5-7") == "+" && get_switch_state_num("AKZHT_Turn13-15") == "+"){
        enable_one_yellow_train(name);
    }
    if (get_switch_state_num("AKZHT_Turn5-7") == "-" && get_switch_state_num("AKZHT_Turn13-15") == "+"){
        enable_two_yellow_train(name);
    }
}

public void return_to_red_after_finishing(string rid)
{
    var AdditionalSignals = new List<string> { "ALB_Sect1-2", "ALB_Sect1-2_2", "ALB_Sect2" };
    if (rid != null)
    {
        var data = route_manager.get_active_routes(rid);
        var a = data.GetValueOrDefault("start");
        var b = data.GetValueOrDefault("end");
        var key = (a, b);
        Console.WriteLine(key);
        var cfg = ROUTE_SIGNAL_MAP.GetValueOrDefault(key);
        foreach (var name in cfg.Keys)
        {
            if (AdditionalSignals.Contains(name)){
                continue;
            }
            if (is_signal_used_by_other_routes(name, rid)){
                continue;
            }
            foreach (var kv in signals_state[name]["lamps"].ToList()){
                var lamp_name = kv.Key;
                var lamp = kv.Value;
                if (lamp_name == "white" && name == "H"){
                    continue;
                }
                set_signal(name, lamp_name, onStatus: false);
                lamp["blink"] = false;
            }
            if (if_color_in_lamp(name, "red")){
                set_signal(name, "red", onStatus: true);
            }
            else if (if_color_in_lamp(name, "blue")){
                set_signal(name, "blue", onStatus: true);
            }
            else{
                foreach (var colors in get_lamp_colors(name)){
                    set_signal(name, colors, onStatus: false);
                }
            }
        }
    }
}

public void invite_signal_on_off(string signalName)
{
    if (get_lamp_state(signalName, "white"))
    {
        set_signal(signalName, "white", onStatus: false);
        sync_simple_CH_with_debug(signalName);
    }
    else if (!get_lamp_state(signalName, "white"))
    {
        set_signal(signalName, "white", onStatus: true);
        sync_simple_CH_with_debug(signalName);
    }
}
#Пока тестовая трёхблочная блокировка
public void after_train_passed_seg((string, string) seg, (string, string) rev)
{
    var signal_segment = segment_to_signal.GetValueOrDefault(seg);
    Console.WriteLine(signal_segment == null ? "None" : string.Join(", ", signal_segment));
    if (signal_segment == null)
    {
        signal_segment = segment_to_signal.GetValueOrDefault(rev, new List<string>());
    }
    foreach (var name in signal_segment)
    {
        if (has_signal_in_states(name))
        {
            foreach (var colors in get_lamp_colors(name))
            {
                if (colors == "red")
                {
                    set_signal(name, colors, onStatus: true);
                }
                else if (colors == "blue")
                {
                    set_signal(name, colors, onStatus: true);
                }
                else
                {
                    set_signal(name, colors, onStatus: false);
                }
            }
        }
    }

    var signals = segment_to_signal.GetValueOrDefault(seg);

    if (signals == null)
    {
        signals = segment_to_signal.GetValueOrDefault(rev, new List<string>());
    }

    if (seg == ("M3", "6") || seg == ("6", "M3"))
    {
        foreach (var (route_id, route_data) in route_manager.get_active_routes_items())
        {
            if (route_data["end"] == "1" || route_data["start"] == "1")
            {
                signals = new List<string>();
            }
        }
    }

    if (seg == ("H", "M1") || seg == ("M1", "H"))
    {
        foreach (var (route_id, route_data) in route_manager.get_active_routes_items())
        {
            if (route_data["end"] == "6" || route_data["start"] == "6")
            {
                signals = new List<string>();
            }
        }
    }

    foreach (var signal_name in signals)
    {
        if (has_signal_in_states(signal_name))
        {
            foreach (var color in get_lamp_colors(signal_name))
            {
                if (color == "red" || color == "blue")
                {
                    set_signal(signal_name, color, onStatus: true);
                }
                else
                {
                    set_signal(signal_name, color, onStatus: false);
                }
            }
        }
    }
}

public void set_signals_to_route(string rid)
{
    if (rid != null)
    {
        var data = route_manager.get_active_routes(rid);
        var a = data.GetValueOrDefault("start");
        var b = data.GetValueOrDefault("end");
        var key = (a, b);
        var cfg = ROUTE_SIGNAL_MAP.GetValueOrDefault(key);
        foreach (var name in cfg.Keys)
        {
            if (name == "H")
            {
                calculate_enter_signal(name);
            }
            else
            {
                foreach (var (color, lamp_cfg) in cfg[name]["lamps"])
                {
                    set_signal(name, color, lamp_cfg["on"]);
                    foreach (var colors in get_lamp_colors(name))
                    {
                        if (colors == "white" && name == "Ч")
                        {
                            continue;
                        }
                        if (cfg[name]["lamps"].ContainsKey(colors))
                        {
                            continue;
                        }
                        else
                        {
                            set_signal(name, colors, false);
                        }
                    }
                }
            }
        }
    }
}

#Флаг фазы мигания
bool signal_blink_phase = false;