using System.Text;

namespace Imperator{
    public class ImperatorRome{
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns>the string after an '=' symbol</returns>
        public static string ResultAfterEquals(string input) {
            int index = input.IndexOf('=');
            if (index == -1) {
                return input;
            }
            return input.Substring(index + 1);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns>the string before an '=' symbol and remove any tabs</returns>
        public static string ResultBeforeEquals(string input) {
            int index = input.IndexOf('=');
            int index2 = input.LastIndexOf('\t') + 1;
            if (index == -1 && index2 == -1) {
                return input;
            }
            if (index2 == -1) {
                return input.Substring(0, index);
            }
            if(index == -1){
                return input.Substring(index2);
            }
            return input.Substring(index2, index - index2);
        }
        public static int EmptyWorld(string gamePath, string outputPath){
            FileInfo[] fileInfos;
            try{    
                fileInfos = new DirectoryInfo(gamePath).GetFiles();
            }catch (Exception){
                throw new Exception("Can't find file path");
            }
            //do for every file
            for(int k = 0; k < fileInfos.Length; k++){
                string[] lines;
                try{
                    lines = File.ReadAllLines(fileInfos[k].FullName);
                }catch (Exception){
                    return 2;
                }
                List<string> outputLines = new List<string>();
                int i = 0;
                while(i < lines.Length){
                    //Parse Lines
                    lines[i] = lines[i].Trim();
                    if(lines[i] == ""){
                        i++;
                        continue;
                    }
                    int openBracketcount = 0;
                    if(lines[i][0] != '#'){
                        i++;
                        continue;
                    }
                    string name = lines[i++].Substring(1);
                    int value = int.Parse(ResultBeforeEquals(lines[i++]));
                    if(lines[i][0] != '{'){
                        return 3;
                    }
                    i++;
                    openBracketcount++;
                    //Break it into segments bases on provience
                    List<string> tokenLines = new List<string>();
                    while(openBracketcount > 0){
                        if(lines[i].Contains('{')){
                            openBracketcount++;
                        }
                        if(lines[i].Contains('}')){
                            openBracketcount--;
                            if(openBracketcount == 0){
                                continue;
                            }
                        }
                        tokenLines.Add(lines[i++]);
                    }
                    Province newProvince = new Province(name, value,tokenLines.ToArray());
                    //modify the states and populations
                    newProvince.Modify();
                    newProvince.Modify2();
                    outputLines.AddRange(newProvince.ToListOfStrings());
                }
                try{
                    File.WriteAllLines(outputPath + fileInfos[k].Name,outputLines.ToArray(),Encoding.UTF8);
                }catch (Exception){
                    return 7;
                }
            }
            return 0;
        }
    }
    public struct Province{
        public string name;
        public int value;
        public Dictionary<string,string> modifiers;
        public List<PopGroup> popGroups = new List<PopGroup>();
        public Province(string name, int value, string[] lines){
            this.name = name;
            this.value = value;
            modifiers = new Dictionary<string, string>();
            int i = 0;
            while(i < lines.Length){
                string begin = ImperatorRome.ResultBeforeEquals(lines[i]);
                string end = ImperatorRome.ResultAfterEquals(lines[i]);
                if(!end.Equals("")){
                    if(!modifiers.TryAdd(begin, end)){
                        //Failed
                        throw new Exception("Error: Duplicate record");
                    }
                    i++;
                    continue;
                }
                //handle only pop groups now
                i++;
                if(!lines[i++].Contains('{')){
                    throw new Exception("Error: Invalid Syntax");
                }
                int amount = 0;
                string? culturePop = null;
                string? religionPop = null;
                while(!lines[i].Contains('}')){
                    if(i >= lines.Length){
                        throw new Exception("Error: with population");
                    }
                    string beforeEquals = ImperatorRome.ResultBeforeEquals(lines[i]);
                    string afterEquals = ImperatorRome.ResultAfterEquals(lines[i++]);
                    //amount can be called multiply times for a single population if source files
                    if(beforeEquals.Equals("amount")){
                        int temp = 0;
                        if(!int.TryParse(afterEquals, out temp)){

                            //there can be a "1f" in the source files
                            afterEquals = afterEquals[1].ToString();
                            int.TryParse(afterEquals, out temp);
                        }
                        amount += temp;
                        continue;
                    }
                    if(beforeEquals.Equals("culture")){
                        culturePop = afterEquals;
                        continue;
                    }
                    if(beforeEquals.Equals("religion")){
                        religionPop = afterEquals;
                        continue;
                    }
                    i++;
                }
                i++;
                popGroups.Add(new PopGroup(begin, amount, culturePop, religionPop));
            }
        }
        /// <summary>
        /// Modify the files in certain ways around provience buildings and other provience stuff
        /// </summary>
        public void Modify(){
            Dictionary<string,string> temp = new Dictionary<string, string>();

            temp.Add("barbarian_power", modifiers["barbarian_power"]);
            temp.Add("civilization_value", modifiers["civilization_value"]);
            temp.Add("culture",modifiers["culture"]);
            string? val;
            if(modifiers.TryGetValue("holy_site", out val)){
                temp.Add("holy_site", val);
            }
            if(modifiers.TryGetValue("port_building", out val)){
                temp.Add("port_building", val);
            }
            val = modifiers["province_rank"];
            if(val != "" || val != "\"settlement\""){
                val = "\"settlement\"";
            }
            temp.Add("province_rank", val);
            temp.Add("religion", modifiers["religion"]);
            temp.Add("terrain",modifiers["terrain"]);
            temp.Add("trade_goods", modifiers["trade_goods"]);
            modifiers = temp;
        }
        /// <summary>
        /// Modify the files in certain ways around population
        /// </summary>
        public void Modify2(){
            for (int i = 0; i < popGroups.Count; i++){
                popGroups.ElementAt(i).SetRank("tribesmen");
                PopGroup temp = popGroups.ElementAt(i);//value passed
                int amount = temp.amount;
                for (int j = i + 1; j < popGroups.Count; j++){
                    PopGroup temp2 = popGroups.ElementAt(j);//value passed
                    if(temp.SameCultureAndReligion(ref temp2)){
                        amount += temp2.amount;
                        popGroups.RemoveAt(j);
                        j--;
                    }
                }
                temp.SetRank("tribesmen");
                temp.SetAmount(amount);
                popGroups.RemoveAt(i);
                popGroups.Insert(i,temp);
            }
        }
        public string[] ToListOfStrings(){
            List<string> output = new List<string>();
            output.Add("#" + name);
            output.Add(value + "=");
            output.Add("{");
            foreach (var item in modifiers){
                output.Add("\t" + item.Key + "=" + item.Value);
            }
            foreach (PopGroup group in popGroups){
                output.AddRange(group.ToListOfStrings());
            }
            output.Add("}");
            return output.ToArray();
        }
    }
    public struct PopGroup{
        public string rank;
        public int amount;
        public string? culture;
        public string? religion;

        public PopGroup(string rank, int amount, string? culture, string? religion){
            this.rank = rank;
            this.amount = amount;
            this.culture = culture;
            this.religion = religion;
        }
        public void SetAmount(int amount){
            this.amount = amount;
        }
        public void SetRank(string rank){
            this.rank = rank;
        }
        public bool SameCultureAndReligion(ref PopGroup other){
            if(culture != other.culture){
                return false;
            }
            if(religion != other.religion){
                return false;
            }
            return true;
        }
        public string[] ToListOfStrings(string cultureProv = "", string religionProv = ""){
            List<string> output = new List<string>();
            output.Add("\t" + rank + "=");
            output.Add("\t{");
            output.Add("\t\tamount=" + amount);
            if(culture != null){
                output.Add("\t\tculture=" + culture);
            }else if(!cultureProv.Equals("")){
                output.Add("\t\tculture=" + cultureProv);
            }
            if(religion != null){
                output.Add("\t\treligion=" + religion);
            }else if(!religionProv.Equals("")){
                output.Add("\t\treligion=" + religionProv);
            }
            output.Add("\t}");
            return output.ToArray();
        }
    }
}