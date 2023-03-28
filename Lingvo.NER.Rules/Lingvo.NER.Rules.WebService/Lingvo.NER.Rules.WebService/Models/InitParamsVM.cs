namespace Lingvo.NER.Rules.WebService
{
    /// <summary>
    /// 
    /// </summary>
    public struct InitParamsVM
    {
        public string Text { get; set; }

        //public bool? ReloadModel { get; set; }

        public bool? ReturnInputText      { get; set; }
        public bool? ReturnUnitedEntities { get; set; }
        public bool? ReturnWordValue      { get; set; }

#if DEBUG
        public override string ToString() => $"ReturnUnitedEntities: {ReturnUnitedEntities}, ReturnWordValue: {ReturnWordValue}, ReturnInputText: {ReturnInputText}, '{Text.Cut()}'"; //, ReloadModel: {ReloadModel}
#endif
    }
}
