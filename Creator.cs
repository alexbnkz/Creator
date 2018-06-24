using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Creator
{
    public class Creator
    {
        private string nmSistema = ConfigurationManager.AppSettings["nmSistema"].ToString();

        private string abreArquivo(string nmFile)
        {
            string texto = "";
            StreamReader arquivo;
            arquivo = File.OpenText(nmFile);
            texto = arquivo.ReadToEnd();
            arquivo.Close();
            return texto;
        }
        private void salvaArquivo(string nmDirectory, string nmFile, string texto)
        {
            if (!Directory.Exists(nmDirectory))
            {
                Directory.CreateDirectory(nmDirectory);
                File.WriteAllText(nmDirectory + nmFile, texto);
            }
            else
            {
                File.WriteAllText(nmDirectory + nmFile, texto);
            }
        }

        public string createSistema(string connString)
        {
            string retorno = "";
            try
            {
                retorno = CreateBase(connString);
                if (retorno == "")
                {
                    retorno = CreateBusiness(connString);
                }
                if (retorno == "")
                {
                    retorno = CreateWebService(connString);
                }
            }
            catch (Exception ex)
            {
                retorno = ex.ToString();
            }
            finally
            {
            }

            if (retorno == "")
            {
                retorno = nmSistema + " criado com sucesso!";
            }


            return retorno;
        }

        public DataSet getTabelaBanco(string connString)
        {
            DataSet ds = new DataSet();
            SqlCommand cmd = null;
            SqlConnection conn = null;

            conn = new SqlConnection(connString);
            cmd = conn.CreateCommand();

            cmd.CommandText += " SELECT NAME ";
            cmd.CommandText += " FROM SYSOBJECTS ";
            cmd.CommandText += " WHERE XTYPE = 'U' AND NAME LIKE '" + ConfigurationManager.AppSettings["dePrefixoTabela"].ToString() + "%' ";
            cmd.CommandText += " ORDER BY NAME ";

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);
            conn.Close();

            return ds;
        }
        public DataSet getCampoBanco(string connString, string tabela)
        {
            DataSet ds = new DataSet();
            SqlCommand cmd = null;
            SqlConnection conn = null;

            conn = new SqlConnection(connString);
            cmd = conn.CreateCommand();

            cmd.CommandText += " SELECT ";
            cmd.CommandText += " COLUNAS.NAME AS NAME, ";
            cmd.CommandText += " TIPOS.NAME AS TIPO, ";
            cmd.CommandText += " TIPOS.XTYPE AS XTIPO, ";
            cmd.CommandText += " COLUNAS.LENGTH AS TAM, ";
            cmd.CommandText += " COLUNAS.ISNULLABLE AS NULO ";
            cmd.CommandText += " FROM SYSOBJECTS AS TABELAS ";
            cmd.CommandText += " INNER JOIN SYSCOLUMNS AS COLUNAS ON TABELAS.ID = COLUNAS.ID ";
            cmd.CommandText += " INNER JOIN SYSTYPES AS TIPOS ON COLUNAS.USERTYPE = TIPOS.USERTYPE ";
            cmd.CommandText += " WHERE TABELAS.NAME = '" + tabela + "' ";
            cmd.CommandText += " AND TABELAS.XTYPE = 'U' ";
            cmd.CommandText += " ORDER BY TIPOS.XTYPE, COLUNAS.ID ";

            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);
            conn.Close();

            return ds;
        }

        private string CreateBase(string connString)
        {
            DataSet dsTabela = new DataSet();

            StringBuilder sb = new StringBuilder();
            string retorno = "";
            string nmModulo = "Base";

            try
            {
                string modulo = abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\" + nmModulo + ".txt");

                string inicio = modulo.Substring(0, modulo.IndexOf("<@TableBegin@>"));
                string meio = modulo.Substring(modulo.IndexOf("<@TableBegin@>"), modulo.IndexOf("<@TableEnd@>") - inicio.Length);
                string fim = modulo.Substring(modulo.IndexOf("<@TableEnd@>"));

                dsTabela = getTabelaBanco(connString);

                foreach (DataRow tabela in dsTabela.Tables[0].Rows)
                {
                    string sFuncoes = meio;

                    // dados da tabela
                    string nmTabela = tabela["NAME"].ToString();
                    string prefixo = ConfigurationManager.AppSettings["dePrefixoTabela"].ToString();
                    string nmClasse = nmTabela.Substring(prefixo.Length);

                    // PK da tabela
                    string identificador = "id_" + nmClasse.ToLower();

                    DataSet dsCampo = getCampoBanco(connString, nmTabela);

                    // SELECT 
                    sb.Remove(0, sb.Length);
                    sb.AppendLine(" cmd.CommandText = \"  SELECT \";");
                    sb.AppendLine(getCampos(" cmd.CommandText += \"     @field", ", \";", "NAME", dsCampo, "") + " \";");
                    sb.AppendLine(" cmd.CommandText += \"  FROM " + nmTabela + " \";");

                    sFuncoes = sFuncoes.Replace("<@CommandTextSelect@>", sb.ToString());
                    sFuncoes = sFuncoes.Replace("<@VerifiedQueryFields@>", getVerifiedQueryFields(dsCampo));

                    // INSERT 
                    sb.Remove(0, sb.Length);
                    sb.AppendLine(" cmd.CommandText = \"  INSERT INTO " + nmTabela + " ( \";");
                    sb.AppendLine(getCampos(" cmd.CommandText += \"     @field", ", \";", "NAME", dsCampo, identificador) + " \";");
                    sb.AppendLine(" cmd.CommandText += \" ) \";");
                    sb.AppendLine(" cmd.CommandText += \" VALUES ( \";");
                    sb.AppendLine(getCampos(" cmd.CommandText += \"     @@field", ", \";", "NAME", dsCampo, identificador) + " \";");
                    sb.AppendLine(" cmd.CommandText += \" ) \";");

                    sFuncoes = sFuncoes.Replace("<@CommandTextInsert@>", sb.ToString());

                    // UPDATE 
                    sb.Remove(0, sb.Length);
                    sb.AppendLine(" cmd.CommandText = \"  UPDATE " + nmTabela + " SET \";");
                    sb.AppendLine(getCampos(" cmd.CommandText += \"     @field = @@field", ", \";", "NAME", dsCampo, identificador) + " \";");
                    sb.AppendLine(" cmd.CommandText += \" WHERE " + identificador + " = @" + identificador + " \";");

                    sFuncoes = sFuncoes.Replace("<@CommandTextUpdate@>", sb.ToString());

                    // DELETE 
                    sb.Remove(0, sb.Length);
                    sb.AppendLine(" cmd.CommandText = \"  DELETE FROM " + nmTabela + "            \";");
                    sb.AppendLine(" cmd.CommandText += \" WHERE " + identificador + " = @" + identificador + " \";");

                    sFuncoes = sFuncoes.Replace("<@CommandTextDelete@>", sb.ToString());

                    sFuncoes = sFuncoes.Replace("<@QueryFields@>", getQueryFields(dsCampo));

                    sFuncoes = sFuncoes.Replace("<@VerifiedFields@>", getVerifiedFields(dsCampo, identificador, nmTabela));

                    sFuncoes = sFuncoes.Replace("<@Table@>", nmClasse);

                    retorno = retorno + sFuncoes;
                }

                retorno = inicio + retorno + fim;
                retorno = retorno.Replace("<@TableBegin@>", "").Replace("<@TableEnd@>", "").Replace("<@nmSistema@>", nmSistema); ;

                string nmDirectory = ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\" + nmSistema + "\\" + nmSistema + "." + nmModulo;
                salvaArquivo(nmDirectory, "\\" + nmModulo + "Access.cs", retorno);
                salvaArquivo(nmDirectory + "\\Properties", "\\AssemblyInfo.cs", abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\Properties\\AssemblyInfo.cs").Replace("<@nmSistema@>", nmSistema).Replace("<@nmModulo@>", nmModulo));

                retorno = "";
            }
            catch (Exception ex)
            {
                retorno = ex.ToString();
            }
            finally
            {
            }
            return retorno;
        }
        private string CreateBusiness(string connString)
        {
            DataSet dsTabela = new DataSet();

            StringBuilder sb = new StringBuilder();
            string retorno = "";
            string nmModulo = "Business";

            try
            {
                string modulo = abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\" + nmModulo + ".txt");

                string inicio = modulo.Substring(0, modulo.IndexOf("<@TableBegin@>"));
                string meio = modulo.Substring(modulo.IndexOf("<@TableBegin@>"), modulo.IndexOf("<@TableEnd@>") - inicio.Length);
                string fim = modulo.Substring(modulo.IndexOf("<@TableEnd@>"));

                dsTabela = getTabelaBanco(connString);

                foreach (DataRow tabela in dsTabela.Tables[0].Rows)
                {
                    string sFuncoes = meio;

                    // dados da tabela
                    string nmTabela = tabela["NAME"].ToString();
                    string prefixo = ConfigurationManager.AppSettings["dePrefixoTabela"].ToString();
                    string nmClasse = nmTabela.Substring(prefixo.Length);

                    // PK da tabela
                    string identificador = "id_" + nmClasse.ToLower();

                    DataSet dsCampo = getCampoBanco(connString, nmTabela);

                    sFuncoes = sFuncoes.Replace("<@Campos@>", getAtributos(" @field", ",", "NAME", dsCampo));

                    sFuncoes = sFuncoes.Replace("<@QueryFields@>", getQueryFields(dsCampo));

                    sFuncoes = sFuncoes.Replace("<@Table@>", nmClasse);

                    retorno = retorno + sFuncoes;
                }

                retorno = inicio + retorno + fim;
                retorno = retorno.Replace("<@TableBegin@>", "").Replace("<@TableEnd@>", "").Replace("<@nmSistema@>", nmSistema);

                string nmDirectory = ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\" + nmSistema + "\\" + nmSistema + "." + nmModulo;
                salvaArquivo(nmDirectory, "\\" + nmModulo + "Access.cs", retorno);
                salvaArquivo(nmDirectory + "\\Properties", "\\AssemblyInfo.cs", abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\Properties\\AssemblyInfo.cs").Replace("<@nmSistema@>", nmSistema).Replace("<@nmModulo@>", nmModulo));

                retorno = "";
            }
            catch (Exception ex)
            {
                retorno = ex.ToString();
            }
            finally
            {
            }
            return retorno;
        }
        private string CreateWebService(string connString)
        {
            DataSet dsTabela = new DataSet();

            StringBuilder sb = new StringBuilder();
            string retorno = "";
            string nmModulo = "WebService";

            try
            {
                string nmFile = ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\" + nmModulo + ".txt";
                string modulo = abreArquivo(nmFile);

                string inicio = modulo.Substring(0, modulo.IndexOf("<@TableBegin@>"));
                string meio = modulo.Substring(modulo.IndexOf("<@TableBegin@>"), modulo.IndexOf("<@TableEnd@>") - inicio.Length);
                string fim = modulo.Substring(modulo.IndexOf("<@TableEnd@>"));

                dsTabela = getTabelaBanco(connString);

                foreach (DataRow tabela in dsTabela.Tables[0].Rows)
                {
                    string sFuncoes = meio;

                    // dados da tabela
                    string nmTabela = tabela["NAME"].ToString();
                    string prefixo = ConfigurationManager.AppSettings["dePrefixoTabela"].ToString();
                    string nmClasse = nmTabela.Substring(prefixo.Length);

                    // PK da tabela
                    string identificador = "id_" + nmClasse.ToLower();

                    DataSet dsCampo = getCampoBanco(connString, nmTabela);

                    sFuncoes = sFuncoes.Replace("<@Campos@>", getAtributos(" @field", ",", "NAME", dsCampo));

                    sFuncoes = sFuncoes.Replace("<@QueryFields@>", getQueryFields(dsCampo));

                    sFuncoes = sFuncoes.Replace("<@Table@>", nmClasse);

                    retorno = retorno + sFuncoes;
                }

                retorno = inicio + retorno + fim;
                retorno = retorno.Replace("<@TableBegin@>", "").Replace("<@TableEnd@>", "").Replace("<@nmSistema@>", nmSistema).Replace("<@WebService@>", "ws" + nmSistema); ;

                string nmDirectory = ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\" + nmSistema + "\\" + nmSistema + "." + nmModulo;
                salvaArquivo(nmDirectory, "\\ws" + nmSistema + ".asmx.cs", retorno);
                salvaArquivo(nmDirectory, "\\ws" + nmSistema + ".asmx", "<%@ WebService Language=\"C#\" CodeBehind=\"ws" + nmSistema + ".asmx.cs\" Class=\"" + nmSistema + ".WebService.ws" + nmSistema + "\" %>");
                salvaArquivo(nmDirectory + "\\Properties", "\\AssemblyInfo.cs", abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\Properties\\AssemblyInfo.cs").Replace("<@nmSistema@>", nmSistema).Replace("<@nmModulo@>", nmModulo));
                salvaArquivo(nmDirectory, "\\Web.config", abreArquivo(ConfigurationManager.AppSettings["deCaminho"].ToString() + "\\Modelo\\WebService.config").Replace("<@connectionStrings@>", connString));

                retorno = "";
            }
            catch (Exception ex)
            {
                retorno = ex.ToString();
            }
            finally
            {
            }
            return retorno;
        }

        public string getCampos(string preLine, string posLine, string field, DataSet DataSet, string identificador)
        {
            int i = 0;
            bool iden = false;
            StringBuilder sb = new StringBuilder();

            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string linha = row[field].ToString();
                if (!iden)
                {
                    if (linha.ToLower() == identificador.ToLower())
                    {
                        iden = true;
                    }
                    else
                    {
                        sb.Append(preLine.Replace("@field", linha));
                        if (i < DataSet.Tables[0].Rows.Count - 1)
                        {
                            sb.AppendLine(posLine);
                        }
                    }
                }
                else
                {
                    sb.Append(preLine.Replace("@field", linha));
                    if (i < DataSet.Tables[0].Rows.Count - 1)
                    {
                        sb.AppendLine(posLine);
                    }
                }
                i++;
            }
            return sb.ToString();
        }
        private string getAtributos(string preLine, string posLine, string field, DataSet DataSet)
        {
            int i = 0;
            StringBuilder sb = new StringBuilder();

            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string linha = row[field].ToString();
                sb.Append(preLine.Replace("@field", trataAtributo(linha)));
                if (i < DataSet.Tables[0].Rows.Count - 1)
                {
                    sb.Append(posLine);
                }
                i++;
            }
            return sb.ToString();
        }

        private string trataAtributo(string atributo)
        {
            string nmAtributo = "";
            bool primeiro = true;
            string[] arr = atributo.Split('_');

            foreach (string trecho in arr)
            {
                if(primeiro) nmAtributo += trecho.ToLower();
                else nmAtributo += trecho.Substring(0, 1).ToUpper() + trecho.Substring(1, trecho.Length - 1).ToLower();
                primeiro = false;
            }
            return nmAtributo;
        }

        private string getQueryFields(DataSet DataSet)
        {
            int i = 0;
            StringBuilder sb = new StringBuilder();

            string[] tipoSql = new string[200];
            tipoSql[0] = "string";      // varchar
            tipoSql[56] = "int";        // int
            tipoSql[61] = "DateTime";   // datetime
            tipoSql[106] = "decimal";   // decimal
            tipoSql[167] = "string";    // varchar

            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string campo = row["NAME"].ToString();
                string atributo = trataAtributo(campo);

                sb.Append(tipoSql[Convert.ToInt32("0" + row["XTIPO"])] + " " + atributo);

                if (i < DataSet.Tables[0].Rows.Count - 1)
                {
                    sb.Append(", ");
                }
                i++;
            }

            return sb.ToString();
        }
        private string getVerifiedQueryFields(DataSet DataSet)
        {
            int i = 0;
            StringBuilder sb = new StringBuilder();

            string[] tipoCrit = new string[200];
            tipoCrit[0] = "\"\"";   // varchar
            tipoCrit[56] = "0";     // int
            tipoCrit[61] = "null";  // datetime
            tipoCrit[106] = "0";    // decimal
            tipoCrit[167] = "\"\""; // varchar

            string[] tipoSql = new string[200];
            tipoSql[0] = "VarChar";     // varchar
            tipoSql[56] = "Int";        // int 
            tipoSql[61] = "DateTime";   // datetime
            tipoSql[106] = "Decimal";   // decimal
            tipoSql[167] = "VarChar";   // varchar

            sb.Append(" if ( ");
            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string campo = row["NAME"].ToString();
                string atributo = trataAtributo(campo);

                sb.Append(atributo + " != " + tipoCrit[Convert.ToInt32("0" + row["XTIPO"])]);

                if (i < DataSet.Tables[0].Rows.Count - 1)
                {
                    sb.Append(" || ");
                }
                i++;
            }
            sb.AppendLine(" ) { ");

            sb.AppendLine("                 bool wAnd = false; ");
            sb.AppendLine("                 cmd.CommandText += \" WHERE \";");

            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string campo = row["NAME"].ToString();
                string atributo = trataAtributo(campo);

                sb.AppendLine("                 if ( " + atributo + " != " + tipoCrit[Convert.ToInt32("0" + row["XTIPO"])] + " ) { ");

                sb.AppendLine("                     if (wAnd) cmd.CommandText += \" AND \"; ");
                sb.AppendLine("                     cmd.CommandText += \" " + campo + " = @" + campo + " \"; ");
                sb.Append("                     cmd.Parameters.Add(new SqlParameter(\"@" + campo + "\",  ");

                if (tipoSql[Convert.ToInt32("0" + row["XTIPO"])] == "VarChar")
                {
                    sb.AppendLine(" SqlDbType." + tipoSql[Convert.ToInt32("0" + row["XTIPO"])] + ", " + row["TAM"] + ")); ");
                }
                else
                {
                    sb.AppendLine(" SqlDbType." + tipoSql[Convert.ToInt32("0" + row["XTIPO"])] + ")); ");
                }

                sb.AppendLine("                     cmd.Parameters[\"@" + campo + "\"].Value = " + atributo + "; ");
                sb.AppendLine("                     wAnd = true; ");
                sb.AppendLine("                 } ");
            }

            sb.AppendLine(" } ");

            return sb.ToString();
        }

        private string getVerifiedFields(DataSet DataSet, string identificador, string tabela)
        {
            StringBuilder sb = new StringBuilder();

            string[] tipoCrit = new string[200];
            tipoCrit[0] = "\"\"";   // varchar
            tipoCrit[56] = "0";     // int
            tipoCrit[61] = "null";  // datetime
            tipoCrit[106] = "0";    // decimal
            tipoCrit[167] = "\"\""; // varchar

            string[] tipoSql = new string[200];
            tipoSql[0] = "VarChar";     // varchar
            tipoSql[56] = "Int";        // int 
            tipoSql[61] = "DateTime";   // datetime
            tipoSql[106] = "Decimal";   // decimal
            tipoSql[167] = "VarChar";   // varchar

            sb.AppendLine("     cmd.Parameters.Add(new SqlParameter(\"@" + identificador + "\", SqlDbType.Int)); ");
            sb.AppendLine("     cmd.Parameters[\"@" + identificador + "\"].Value = <@Table@>." + trataAtributo(identificador) + "; ");
            sb.AppendLine("");
            sb.AppendLine("     if (operacao == \"INC\" || operacao == \"ALT\") { ");

            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                string campo = row["NAME"].ToString();
                string atributo = trataAtributo(campo);

                if (campo != identificador)
                {
                    sb.Append("     cmd.Parameters.Add(new SqlParameter(\"@" + campo + "\",  ");
                    if (tipoSql[Convert.ToInt32("0" + row["XTIPO"])] == "VarChar")
                    {
                        sb.AppendLine("SqlDbType." + tipoSql[Convert.ToInt32("0" + row["XTIPO"])] + ", " + row["TAM"] + ")); ");
                    }
                    else
                    {
                        sb.AppendLine("SqlDbType." + tipoSql[Convert.ToInt32("0" + row["XTIPO"])] + ")); ");
                    }

                    if (Convert.ToInt32("0" + row["NULO"]) == 0)
                    {
                        sb.AppendLine("     cmd.Parameters[\"@" + campo + "\"].Value = <@Table@>." + atributo + "; ");
                    }
                    else
                    {
                        sb.AppendLine("     if ( <@Table@>." + atributo + " != " + tipoCrit[Convert.ToInt32("0" + row["XTIPO"])] + " ) ");
                        sb.AppendLine("     cmd.Parameters[\"@" + campo + "\"].Value = <@Table@>." + atributo + "; ");
                        sb.AppendLine("     else cmd.Parameters[\"@" + campo + "\"].Value = DBNull.Value; ");
                    }
                }
                sb.AppendLine("");
            }

            sb.AppendLine("     cmd.Transaction = conn.BeginTransaction(); ");
            sb.AppendLine("     wOk = (cmd.ExecuteNonQuery() > 0); ");
            sb.AppendLine("     } ");
            sb.AppendLine("     else if (operacao == \"EXC\") { ");
            sb.AppendLine("");
            sb.AppendLine("         // Verificar todas as tabelas que tem relacionamento ");
            sb.AppendLine("         bool wDeleta = true; ");
            sb.AppendLine("");
            sb.AppendLine("         //wDeleta = wDeleta && !RetornaSelect(\"SELECT " + identificador + " FROM XX_TABELA_ESTRANGEIRA_XX WHERE " + identificador + " = '\" + <@Table@>." + trataAtributo(identificador) + " + \"' \"); ");
            sb.AppendLine("         if (!wDeleta) retorno = \"Esse registro tem registros relacionados em XXX!\"; ");
            sb.AppendLine("");
            sb.AppendLine("         cmd.Transaction = conn.BeginTransaction(); ");
            sb.AppendLine("         wOk = (wDeleta) && (cmd.ExecuteNonQuery() > 0); ");
            sb.AppendLine("     } ");
            sb.AppendLine("");
            sb.AppendLine("     if (wOk) cmd.Transaction.Commit(); ");
            sb.AppendLine("     else cmd.Transaction.Rollback(); ");
            sb.AppendLine("     conn.Close(); ");

            return sb.ToString();
        }

    }
}