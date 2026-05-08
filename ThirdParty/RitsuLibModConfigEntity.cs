using RegentFX.Scripts;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable CollectionNeverQueried.Global

namespace RegentFX.ThirdParty;

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