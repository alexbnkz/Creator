using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using Creator;

namespace Creator
{
    public partial class Default : System.Web.UI.Page
    {
        Creator cre = new Creator();

        private static string getConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["SQLConnection"].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            deCaminho.Text = ConfigurationManager.AppSettings["deCaminho"].ToString();
            nmSistema.Text = ConfigurationManager.AppSettings["nmSistema"].ToString();
            deConnectionString.Text = getConnectionString();
            dePrefixoTabela.Text = ConfigurationManager.AppSettings["dePrefixoTabela"].ToString();

            System.Data.DataSet ds = cre.getTabelaBanco(getConnectionString());

            ds.Tables[0].Columns[0].ColumnName = "Tabela";
            ds.Tables[0].Columns.Add("Campos");

            gvBanco.DataSource = ds;
            gvBanco.DataBind();
        }

        protected void btCreate_Click(object sender, EventArgs e)
        {
            Response.Write(cre.createSistema(getConnectionString()));
        }

        protected void gvBanco_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                string nmTabela = e.Row.Cells[0].Text;
                string prefixo = ConfigurationManager.AppSettings["dePrefixoTabela"].ToString();
                string nmClasse = nmTabela.Substring(prefixo.Length);

                string identificador = "id_" + nmClasse.ToLower();

                e.Row.Cells[1].Text = cre.getCampos("@field", ", ", "NAME", cre.getCampoBanco(getConnectionString(), e.Row.Cells[0].Text), "").ToLower();

                e.Row.Cells[1].Text = e.Row.Cells[1].Text.ToLower().Replace(identificador + ",", "<strong>" + identificador + "</strong>,");

            }
        }
    }
}