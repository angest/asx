namespace asx
{
    class Company
    {
        public string name_full { get; set; }
        public string code { get; set; }
        public string industry_group_name { get; set; }
        public PrimaryShare primary_share { get; set; }

        public Company(string name_full, string code, string industry_group_name)
        {
            this.name_full = name_full;
            this.code = code;
            this.industry_group_name = industry_group_name;
        }
    }

    class PrimaryShare
    {
        public decimal last_price { get; set; }
        public decimal pe { get; set; }
        public decimal eps { get; set; }
    }
}
