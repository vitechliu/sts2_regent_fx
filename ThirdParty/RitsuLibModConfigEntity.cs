using RegentFX.Scripts;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RegentFX.ThirdParty;

/**
 * return """
               {
                 "modId": "RegentFX",
                 "modDisplayName": "RegentFX 万象辉星",
                 "modSidebarOrder": 50,
                 "pages": [
                   {
                     "pageId": "main",
                     "title": "主要设置",
                     "description": "RegentFX 主设置",
                     "sortOrder": 1000,
                     "sections": [
                       {
                         "id": "core",
                         "title": "特效",
                         "entries": [
                           {
                             "id": "master_vol",
                             "type": "slider",
                             "key": "ExposureThreshold",
                             "label": "光效强度 Light Exposure Setting",
                             "description": "设置为0将关闭光效",
                             "min": 0,
                             "max": 2,
                             "step": 0.05,
                             "scope": "global"
                           }
                         ]
                       }
                     ]
                   }
                 ]
               }
               """;
 */
public struct RitsuLibModConfigEntity {
  public string modId { get; set; } = Entry.ModId;
  public string modDisplayName { get; set; }
  public int modSidebarOrder { get; set; } = 50;

  public List<RLMCPage> pages { get; set; } = new();
  public RitsuLibModConfigEntity() {
  }
}

public struct RLMCPage() {
  public string pageId { get; set; }
  public string title  { get; set; }
  public string description  { get; set; }
  public int sortOrder { get; set; } = 50;
  public List<RLMCSection> sections { get; set; } = new ();
}

public struct RLMCSection() {
  public string id { get; set; }
  public string title { get; set; }
  public List<object> entries { get; set; } = new ();
}


public struct SliderEntry() {
  public string id { get; set; }
  public readonly string type { get; } = "slider";
  public string key { get; set; }
  public string label { get; set; }
  public string description { get; set; }
  public double min { get; set; }
  public double max { get; set; }
  public double step { get; set; }
  public RLMCScope scope { get; set; } = RLMCScope.global;
}

public struct ToggleEntry() {
  public string id { get; set; }
  public readonly string type { get; } = "toggle";
  public string key { get; set; }
  public string label { get; set; }
  public string description { get; set; }
  public RLMCScope scope { get; set; } = RLMCScope.global;
}


public enum RLMCScope {
  global,
  profile,
}