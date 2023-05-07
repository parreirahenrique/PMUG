using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ProgMntUG
{
    // Classe para Construção de Sistemas
    # region Classe para construção de sistema teste
    public class DadosSistema
    {
        // ------------------------------------------------
        // Atributos da Classe
        public int numTeste = new int();                                  // Número (contador) de testes
        public string sistemaTeste;                                       // Nome do sistema a ser utilizado no teste
        public string periodoEst;                                         // Período (intervalo) de estudo
        public int semIniMnt = new int();                                 // Semana inicial do período de manutenção do cada teste
        public int semFimMnt = new int();                                 // Semana fim do período de manutenção do cada teste (inclui a própria semana)
        public List<Gerador> Geradores = new List<Gerador>();             // Lista com todas as unidades geradoras do sistema
        public List<Gerador> GeradoresMnt = new List<Gerador>();          // Lista com as unidades geradoras que devem ser programadas para manutenção
        public List<Gerador> GeradoresOrdCusto = new List<Gerador>();     // Lista com todas as unidades geradoras do sistema ordenadas pelo custo
        public List<Gerador> GeradoresMntOrdCapGer = new List<Gerador>(); // Lista com as unidades geradoras que devem ser programadas para manutenção ordenadas pela capacidade de geração
        public List<Gerador> GeradoresJaProgMnt = new List<Gerador>();    // Lista com unidades geradoras que já estão programadas para manunteção dentro deste período de estudo
        public List<int> nGerUsinMnt = new List<int>();                   // Lista com número de geradores por usina que devem ser programados para manutenção

        public int nGerSistema = new int();                               // Número de geradores que compõem o sistema (que podem falhar durante a operação)
        public int nGerProgMnt = new int();                               // Número de geradores que deverão ser programados para manutenção dentro do período de estudo
        public int nGerJaProgMnt = 0;                                     // Número de geradores que já foram previamente programados para manutenção dentro do período de estudo

        public double cargaTotal = new double();                          // Carga total instalada do sistema
        public double[] curvaCarga;                                       // Curva de carga para perído de estudo

        public int nHorasPer = 0;                                         // Número total de horas do período de estudo

        public int contAvalConf = 0;                                      // Conta o número de avaliações de confiabilidade realizadas no estudo
        public double nEstadosConv = 0;                                   // Número de estados necessários para convergência da SMCNS
        public double betaTol;                                            // Tolerância para convergência da SMCNS

        public Testes testeAtual = new Testes();                          // Objeto com parâmetros do teste atual

        public double[] PLDMedioSemanal = new double[53];                 // PLD médio para as 53 semanas do ano [R$/MWh]

        List<Plano> MelhoresResultadosNSGA = new List<Plano>();           // Lista para armazenar os três melhores resultados, um para cada função objetivo, para comparações futuras das soluções da NSGA-II

        // ------------------------------------------------
        // Método construtor do sistema
        public DadosSistema()
        {
        }

        // ------------------------------------------------
        // Método para leitura/carregamento do sistema
        public void LeituraDadosEntrada(Testes _testeAtual, string _nomePasta)
        {
            // Armanzenando informações do teste atual
            this.numTeste = _testeAtual.numTeste;
            this.sistemaTeste = _testeAtual.sistemaTeste;
            this.periodoEst = _testeAtual.periodoEst;
            this.semIniMnt = _testeAtual.semIniMnt;
            this.semFimMnt = _testeAtual.semFimMnt;
            this.nHorasPer = (_testeAtual.semFimMnt - _testeAtual.semIniMnt + 1) * 168;
            this.betaTol = _testeAtual.betaTol;

            this.testeAtual = _testeAtual;

            // ------------------------------------------------
            // Abrindo arquivo com dados do sistema para leitura
            #region Leitura dos dados do sistema em estudo
            StreamReader fileSist = new StreamReader(@"" + "DADOS DE ENTRADA" + "//" + sistemaTeste + "_PRODUTOR.txt");

            // Leitura das informações do arquivo do sistema e criação dos geradores
            string lineArq = fileSist.ReadLine();            // Criando variável para leitura de cada linha do arquivo e lendo primeira a linha
            lineArq = fileSist.ReadLine();                   // Leitura de nova linha do arquivo
            lineArq = fileSist.ReadLine();                   // Leitura de nova linha do arquivo
            lineArq = fileSist.ReadLine();                   // Leitura de nova linha do arquivo
            int contGer = 0;                                 // Contador de geradores do sistema teste
            nGerProgMnt = 0;
            while (lineArq != "####")
            {
                // Lendo linha de nova usina
                Gerador ger;
                int contGerMnt = 0;                             // Contador de geradores dentro da usina que entram para manutenção
                string[] dadosLinhaArq = lineArq.Split('\t');   // Vetor com informações separadas
                nGerProgMnt = nGerProgMnt + Convert.ToInt16(dadosLinhaArq[6]);
                nGerSistema = nGerSistema + Convert.ToInt16(dadosLinhaArq[1]);
                int numeroGerUsina = Convert.ToInt16(dadosLinhaArq[1]);
                for (int i = 0; i < numeroGerUsina; i++)
                {
                    // Criando novo gerador
                    contGer++;
                    bool gerMnt;
                    int posvetorPlano;
                    if (contGerMnt < Convert.ToInt16(dadosLinhaArq[6]))
                    {
                        gerMnt = true;                   // Gerador deverá ser programado para manutenção dentro do período em estudo
                        contGerMnt++;
                        posvetorPlano = (nGerProgMnt - (Convert.ToInt16(dadosLinhaArq[6]) - contGerMnt)) - 1;  // Definindo para o gerador a sua posição dentro do vetor plano de manutenção
                    } else
                    {
                        gerMnt = false; // Gerador não deverá ser programado para manutenção dentro do período em estudo
                        posvetorPlano = -1;              // Flag que dirá que gerador não será programado para manutenção
                    }
                    ger = new Gerador(contGer, Convert.ToInt16(dadosLinhaArq[0]), Convert.ToDouble(dadosLinhaArq[2]) / 100, Convert.ToDouble(dadosLinhaArq[3]),
                            Convert.ToDouble(dadosLinhaArq[4]), Convert.ToDouble(dadosLinhaArq[5]), Convert.ToInt16(dadosLinhaArq[7]), gerMnt, posvetorPlano);
                    Geradores.Add(ger);
                    if (posvetorPlano > -1) { GeradoresMnt.Add(ger); }
                }
                if (Convert.ToInt16(dadosLinhaArq[6]) > 0) { nGerUsinMnt.Add(Convert.ToInt16(dadosLinhaArq[6])); }
                lineArq = fileSist.ReadLine();        // Leitura de nova linha do arquivo
            }
            fileSist.Close();
            #endregion

            // ------------------------------------------------
            // Abrindo arquivo com informações dos geradores previamente programados para manutenção
            #region Leitura geradores já programados para manunteção
            StreamReader fileGerJaProg = new StreamReader(@"" + "DADOS DE ENTRADA" + "//bGeradores_Ja_Programados.txt");
            TextWriter fileOut = new StreamWriter(new FileStream(@"" + _nomePasta + "//bGeradores_Ja_Programados.txt", FileMode.Create));   // Criando cópia dos dados de entrada (parâmetros) na pasta de saída


            // Leitura das informações do arquivo do sistema e criação dos geradores
            string lineArqGerJaProg = fileGerJaProg.ReadLine();   // Criando variável para leitura de cada linha do arquivo e lendo primeira a linha
            fileOut.WriteLine(lineArqGerJaProg);
            lineArqGerJaProg = fileGerJaProg.ReadLine(); fileOut.WriteLine(lineArqGerJaProg);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            lineArqGerJaProg = fileGerJaProg.ReadLine(); fileOut.WriteLine(lineArqGerJaProg);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            lineArqGerJaProg = fileGerJaProg.ReadLine(); fileOut.WriteLine(lineArqGerJaProg);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            contGer = 0;                                                                              // Contador de geradores
            while (lineArqGerJaProg != "####")
            {
                string[] dadosLinhaArq = lineArqGerJaProg.Split('\t');     // Vetor com informações separadas
                if (Convert.ToInt16(dadosLinhaArq[3]) == 1)
                { // Gerador já programado para manutenção
                    for (int i = contGer; i < Geradores.Count(); i++)
                    {
                        if (Geradores[i].nUsina == Geradores[contGer].nUsina && Geradores[i].progMnt == false && Geradores[i].gerJaProgMnt == false)
                        {
                            Geradores[i].gerJaProgMnt = true;
                            Geradores[i].semMntUGJaProgr = Convert.ToInt16(dadosLinhaArq[4]);
                            this.nGerJaProgMnt++;
                            break;
                        }
                    }
                }
                contGer++;
                lineArqGerJaProg = fileGerJaProg.ReadLine(); fileOut.WriteLine(lineArqGerJaProg);     // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            }
            while ((lineArqGerJaProg = fileGerJaProg.ReadLine()) != null) { fileOut.WriteLine(lineArqGerJaProg); }
            fileGerJaProg.Close();
            fileOut.Close();
            #endregion

            // ------------------------------------------------
            // Abrindo arquivo com informações do PLD semanal
            #region Leitura PLD semanal
            StreamReader filePLD = new StreamReader(@"" + "DADOS DE ENTRADA" + "//PLD_SEMANAL.txt");
            TextWriter filePLDOut = new StreamWriter(new FileStream(@"" + _nomePasta + "//PLD_SEMANAL.txt", FileMode.Create));   // Criando cópia dos dados de entrada (parâmetros) na pasta de saída


            // Leitura das informações do arquivo
            string lineArqPLD = filePLD.ReadLine();   // Criando variável para leitura de cada linha do arquivo e lendo primeira a linha
            filePLDOut.WriteLine(lineArqPLD);
            lineArqPLD = filePLD.ReadLine(); filePLDOut.WriteLine(lineArqPLD);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            lineArqPLD = filePLD.ReadLine(); filePLDOut.WriteLine(lineArqPLD);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            lineArqPLD = filePLD.ReadLine(); filePLDOut.WriteLine(lineArqPLD);         // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            int  contSemana = 0;                                                       // Contador de semanas
            while (lineArqPLD != "####")
            {
                string[] dadosLinhaArq = lineArqPLD.Split('\t');                       // Vetor com informações separadas
                PLDMedioSemanal[contSemana] = Convert.ToDouble(dadosLinhaArq[1]);
                contSemana++;
                lineArqPLD = filePLD.ReadLine(); filePLDOut.WriteLine(lineArqPLD);     // Leitura de nova linha do arquivo (escrita no arquivo cópia)
            }
            while ((lineArqPLD = filePLD.ReadLine()) != null) { filePLDOut.WriteLine(lineArqPLD); }
            filePLD.Close();
            filePLDOut.Close();
            #endregion

            // ------------------------------------------------
            // Abrindo arquivo com dados da carga para leitura
            #region Leitura da curva de carga
            StreamReader fileCarga = new StreamReader(@"" + "DADOS DE ENTRADA" + "//" + "CARGA_SEeCO_HORA.txt");
            if (_testeAtual.curvaDeCarga == "PICO")
            {
                fileCarga = new StreamReader(@"" + "DADOS DE ENTRADA" + "//" + "CARGA_SEeCO_PICO.txt");
            }

            // Leitura das informações do arquivo de carga
            lineArq = fileCarga.ReadLine();                                    // Reinstanciando variável para leitura de cada linha do arquivo e lendo primeira a linha
            cargaTotal = Convert.ToDouble(lineArq);                            // Guardando informação de carga total do sistema
            curvaCarga = new double[_testeAtual.nSemPerEst * 7 * 24];          // Instanciando curva de carga com tamanho de acordo com número de horas do período de estudo
            int horaInicio = _testeAtual.semIniMnt * 7 * 24 - (7 * 24) + 1;    // Hora de início do período de estudo
            int horaFim = _testeAtual.semFimMnt * 7 * 24;                      // Hora de término do período de estudo
            int contHoras = 0;                                                 // Contador de horas dentro da curva de carga
            while ((lineArq = fileCarga.ReadLine()) != null)
            {
                contHoras++;
                if (contHoras >= horaInicio && contHoras <= horaFim)
                {
                    // Nível de carga
                    curvaCarga[contHoras - horaInicio] = Convert.ToDouble(lineArq) / 100;
                }
            }
            // Rotina pequena só para verificar se último nível de carga está correto
            // double ultimoNivel = curvaCarga[curvaCarga.Length-1];
            // ultimoNivel++;
            fileCarga.Close();
            #endregion

            // ------------------------------------------------
            // Criando lista de geradores ordenada pelo custo de geração
            contGer = 0;
            double[] custosGer = new double[Geradores.Count()];    // Vetor custo de geração para ordenação
            int[] ordem = new int[Geradores.Count()];              // Geradores ordenados pelo custo
            foreach (Gerador gerador in Geradores)
            {
                custosGer[contGer] = gerador.custoGer;
                ordem[contGer] = contGer;
                contGer++;
            }
            Array.Sort(custosGer, ordem);
            for (int i = 0; i < Geradores.Count(); i++)
            {
                GeradoresOrdCusto.Add(Geradores[ordem[i]]);
            }

            // ------------------------------------------------
            // Criando lista de geradores para manutenção ordenada pela capacidade de geração (decrescente)
            contGer = 0;
            double[] capGer = new double[GeradoresMnt.Count()];    // Vetor capacidade de geração para ordenação
            ordem = new int[GeradoresMnt.Count()];                 // Geradores ordenados pelo custo
            foreach (Gerador gerador in GeradoresMnt)
            {
                capGer[contGer] = gerador.PotMax;
                ordem[contGer] = contGer;
                contGer++;
            }
            Array.Sort(capGer, ordem);
            Array.Reverse(ordem);
            for (int i = 0; i < GeradoresMnt.Count(); i++)
            {
                GeradoresMntOrdCapGer.Add(GeradoresMnt[ordem[i]]);
            }
        }
    }
    # endregion

    // Classe para criação de Unidades Geradoras
    # region Classe para criação de objetos do tipo Gerador
    public class Gerador
    {
        // Atributos da Usina
        public int ID;                       // Identificador da Gerador
        public int nUsina;                   // Número da usina que contém o gerador
        public double FOR;                   // Taxa de saída forçada (FOR)
        public double FOR_CE;                // Taxa de saída forçada para Entropia Cruzada (parâmetro a ser "distorcido")
        public double PotMax;                // Potência máxima do gerador [MW]
        public double custoGer;              // Custo de geração [1000000 x $/MW]
        public double MTTR;                  // Tempo médio de reparo do gerador [h]
        public int nSemNecMnt;               // Número de semanas que o gerador necessita ficar em manutenção
        public bool progMnt;                 // Flag que define se gerador será programado para manutenção dentro do período em estudo
        public int posvetorPlano;            // Se gerador deve ser programado para manutenção dentro do período de estudo, este atributo define sua posição no vetor plano

        public int estadoOperativo = 1;      // Estado operativo do gerador (0 = falhado/manutenção (indisponível); 1 = operando (disponível))
        public int estadoOperativoFalha = 1; // Estado operativo do gerador (0 = falhado (indisponível); 1 = operando (disponível)) - Apenas falha

        public int horaIniMnt;               // Hora de início da manutenção de um gerador se já programado (no processo de otimização)
        public int horaFimMnt;               // Hora de término da manutenção de um gerador se já programado (no processo de otimização)

        public double[] eesSemanal;          // Vetor com valores esperados de energia suprida (EES) pelo gerador em cada semana do período de estudo
        public double bonusGer = 0;          // Variável bônus para função objetivo do problema de otimização (só recebe valor diferente de zero se o gerador tiver que entrar em manutenção)

        public bool gerJaProgMnt = false;    // Flag que indica se unidade geradora já foi previamente programada (fora do processo de otimização)
        public int semMntUGJaProgr = 0;      // Semana de manutenção da unidade geradora já previamente programada (programação informada nos dados de entrada)

        public double receitaMedia = 0;      // Receita média do gerador no período de estudo [R$]

        // Método construtor da usina
        public Gerador(int _ID, int _nUsina, double _FOR, double _PotMax, double _custoGer, double _MTTR, int _nSemNecMnt, bool _progMnt, int _posvetorPlano)
        {
            this.ID = _ID;
            this.nUsina = _nUsina;
            this.FOR = _FOR;
            this.PotMax = _PotMax;
            this.custoGer = _custoGer;
            this.MTTR = _MTTR;
            this.nSemNecMnt = _nSemNecMnt;
            this.progMnt = _progMnt;
            this.posvetorPlano = _posvetorPlano;
        }

        // Método para reescrever os dados do gerador
        public override string ToString()
        {
            return "ID: " + ID.ToString("#00") + "   nUsina: " + nUsina.ToString("#00") + "   FOR: " + FOR.ToString("#0.00") + "   FOR_CE: " + FOR_CE.ToString("#0.0000") +
                "   PotMax: " + PotMax.ToString("#000.00") + "   CustoGer: " + (1000 * custoGer).ToString("#000.00") + "   MTTR: " + MTTR.ToString("#000.00") +
                "   nSemNecMnt: " + nSemNecMnt.ToString("#0") + "   progMnt: " + progMnt.ToString() + "   horaIniMnt: " + horaIniMnt.ToString() +
                "   estOperativo: " + estadoOperativo.ToString() + "   posVetorPlano: " + posvetorPlano.ToString();
        }
    }
    # endregion
}
