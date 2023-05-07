using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace ProgMntUG
{
    class Program
    {
        static void Main(string[] args)
        {
            // ------------------------------------------------------
            // Pasta para resultados de saída
            # region Pasta (diretório) para resultados de saída
            string nomePasta = " ";
            DateTime data = DateTime.Now;
            string dataAtual = string.Format("{0:dMyyyy-HHmmss}", data);
            string diretorio = Environment.CurrentDirectory;
            nomePasta = @diretorio + "\\RESULT-" + dataAtual;
            Directory.CreateDirectory(nomePasta);
            DirectoryInfo ndir = new DirectoryInfo(nomePasta);
            // limpa diretorio temporario...
            FileInfo[] arquivos = ndir.GetFiles("*.*");
            foreach (FileInfo arq in arquivos) { arq.Delete(); }
            # endregion

            // -------------------------------------------------------
            // Lendo arquivo com parâmetros de entrada (testes) para o estudo
            # region Leitura dos parâmetros de entrada (testes)
            StreamReader fileIn = new StreamReader(@"" + "DADOS DE ENTRADA" + "//aParametros_Entrada.txt");                                                       // Abertura do arquivo de entrada
            TextWriter fileOut = new StreamWriter(new FileStream(@"" + nomePasta + "//aParametros_Entrada.txt", FileMode.Create));   // Criando cópia dos dados de entrada (parâmetros) na pasta de saída

            // Leitura das informações do arquivo e criação dos geradores
            string lineArq = fileIn.ReadLine();                      // Criando variável para leitura de cada linha do arquivo e lendo primeira a linha
            fileOut.WriteLine(lineArq);

            Testes teste;
            lineArq = fileIn.ReadLine();
            fileOut.WriteLine(lineArq);
            lineArq = fileIn.ReadLine();
            fileOut.WriteLine(lineArq);
            lineArq = fileIn.ReadLine();
            fileOut.WriteLine(lineArq);
            lineArq = fileIn.ReadLine();
            fileOut.WriteLine(lineArq);
            List<Testes> ListaTestes = new List<Testes>();         // Lista de testes a serem realizados
            int contTeste = 0;                                     // Contador de quantidade de testes
            while (lineArq != "####")
            {
                // Lendo informações de novo teste
                contTeste++;
                teste = new Testes();
                string[] dadosLinhaArq = lineArq.Split('\t');      // Vetor com informações separadas
                teste.numTeste = contTeste;
                teste.semente = Convert.ToInt16(dadosLinhaArq[1]);
                teste.sistemaTeste = dadosLinhaArq[2];
                teste.periodoEst = dadosLinhaArq[3];
                if (teste.periodoEst == "ANO") { teste.semIniMnt = 1; teste.semFimMnt = 52; }      // Período de estudo anual
                if (teste.periodoEst == "INV") { teste.semIniMnt = 1; teste.semFimMnt = 13; }      // Período de estudo por estação - Inverno
                if (teste.periodoEst == "PRI") { teste.semIniMnt = 14; teste.semFimMnt = 26; }     // Período de estudo por estação - Primavera
                if (teste.periodoEst == "VER") { teste.semIniMnt = 27; teste.semFimMnt = 39; }     // Período de estudo por estação - Verão
                if (teste.periodoEst == "OUT") { teste.semIniMnt = 40; teste.semFimMnt = 52; }     // Período de estudo por estação - Outono    
                if (teste.periodoEst == "SECO") { teste.semIniMnt = 18; teste.semFimMnt = 48; }    // Período de estudo por estação - Seco (de começo de Maio a fim de Novembro)
                if (teste.periodoEst == "UMID") { teste.semIniMnt = 17; teste.semFimMnt = 49; }    // Período de estudo por estação - Seco (de começo de Dezembro a fim de Abril). Há aqui a inversão proposital das variáveis de início e de fim
                
                teste.nSemPerEst = (teste.semFimMnt + 1) - teste.semIniMnt;
                teste.nExec = Convert.ToInt16(dadosLinhaArq[4]);
                teste.reservaGer = Convert.ToDouble(dadosLinhaArq[5]);                             // Reserva de geração ativa a ser considerada no estudo (MW)
                teste.UC = Convert.ToDouble(dadosLinhaArq[6]);                                     // Custo do corte de carga para LOLC ($/MWh)

                // Leitura dos parâmetros para SMCNS
                teste.betaTol = Convert.ToDouble(dadosLinhaArq[7]) / 100;                          // Tolerância para convergência da SMCNS
                teste.utilizaCE = Convert.ToBoolean(dadosLinhaArq[8]);                             // Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade? (true or false)

                // Leitura dos parâmetros para técnica de otimização
                teste.tecOtimizacao = (dadosLinhaArq[9]);                                          // Técnica de otimização a ser utilizada (EE or AG)
                teste.numInd = Convert.ToInt16(dadosLinhaArq[10]);                                 // Tamanho da população de indíviduos
                teste.gerMax = Convert.ToInt16(dadosLinhaArq[11]);                                 // Número máximo de gerações
                teste.repMax = Convert.ToInt16(dadosLinhaArq[12]);                                 // Número máximo de repetições p/ a melhor solução
                teste.sigma = Convert.ToDouble(dadosLinhaArq[13]);                                 // Passo de mutação
                teste.numEleMut = Convert.ToInt16(dadosLinhaArq[14]);                              // Número de elementos que sofreram mutação no vetorPlano de cada individuo a cada geração
                teste.curvaDeCarga = dadosLinhaArq[15];                                            // Curva de carga a ser considerada no estudo (PICO ou HORA)
                teste.nomePasta = nomePasta;                                                      // Diretório do teste atual salvo na variável do sistema

                ListaTestes.Add(teste);
                lineArq = fileIn.ReadLine();                       // Leitura de nova linha do arquivo
                fileOut.WriteLine(lineArq);
            }
            while ((lineArq = fileIn.ReadLine()) != null) { fileOut.WriteLine(lineArq); }
            fileIn.Close();
            fileOut.Close();
            # endregion

            // -------------------------------------------------------
            // Execução dos Testes
            # region Execução dos Testes
            foreach (Testes testeAtual in ListaTestes)
            {
                // -------------------------------------------------------
                // Construindo sistema de estudo
                DadosSistema sistema = new DadosSistema();
                sistema.LeituraDadosEntrada(testeAtual,nomePasta);
                

                // -------------------------------------------------------
                // Executando rotina de otimização
                RotOtimizacao MetodOtimiz = new RotOtimizacao(sistema, 11111);            // Criando objeto da rotina de otimização
                List<Plano> topTenPlanosExec = new List<Plano>();                         // Lista de 10 melhores planos identificados a cada execução realizada
                List<List<Plano>> listaTodosTopTenPlanosExec = new List<List<Plano>>();   // Lista de 10 melhores planos identificados a cada execução realizada
                List<List<Plano>> listaTodosPlanosNSGA = new List<List<Plano>>();         // Lista de planos com todos os indivíduos de cada execução da NSGA-II
                List<Plano> melhoresSolucoes = new List<Plano>();                         // Lista contendo a melhor solução para cada um dos objetivos para comparações da NSGA-II
                double nMedioAvalConf = 0;                                                // Número médio de avaliações de confiabilidade
                double nMedioEstaConv = 0;                                                // Número médio de estados para convergência da SMCNS
                double nMedioGeracoes = 0;                                                // Número médio de gerações/iterações da metaheurística
                double nMedioRepeticoes = 0;                                              // Número médio de repetições da metaheurística
                int sementeAtual = testeAtual.semente;

                // Valores médios da EE(Lambda+Mi)
                double gerMedio = 0;                                                      // Número médio de gerações
                double repMedio = 0;                                                      // Número médio de repetições da melhor solução

                // Loop de execuções
                string[] TemposExecucoes = new string[testeAtual.nExec];
                
                // Caso o teste atual seja uma otimização multi-objetivo via NSGA-II, primeiro faz-se a otimização de cada um dos objetivos para futuras comparações
                //if(testeAtual.tecOtimizacao == "NSGA-II")
                //{
                    //Console.WriteLine(" ----------------------------------------------------------------------");
                    //Console.WriteLine(" OTIMIZANDO PLANOS DE MANUTEÇÃO PARA CADA UMA DAS FUNÇÕES OBJETIVO - AG");
                    //Console.WriteLine(" ");

                    //Plano bestSolution;                                // Objeto para conter o melhor plano de manutenção do i-ésimo objetivo

                    //// Otimizando para cada um dos três objetivos
                    //for(int i = 1; i <= 3; i++)
                    //{
                        //bestSolution = new Plano(sistema);             // Reiniciando objeto para armazenar o i-ésimo plano de manutenção
                        //bestSolution = MetodOtimiz.OtimizacaoAG(i);    // Otimizando via AG para o i-ésimo objetivo
                        //melhoresSolucoes.Add(bestSolution);            // Armazenando plano de manutenção na lista
                    //}
                //}
                
                for (int i = 0; i < testeAtual.nExec; i++)
                {
                    Console.WriteLine(" ----------------------------------------------------------------------");
                    Console.WriteLine(" TESTE - " + testeAtual.numTeste + " - Execução - " + (i + 1) + " - " + sistema.periodoEst + " - " + sistema.testeAtual.tecOtimizacao);
                    Console.WriteLine(" Semente utilizada - " + sementeAtual);
                    // Rotina de otimização
                    sistema.nEstadosConv = 0;
                    MetodOtimiz = new RotOtimizacao(sistema, sementeAtual);                // Instanciando objeto da rotina de otimização
                    TimeSpan dI = Process.GetCurrentProcess().TotalProcessorTime;          // Início da contagem de tempo da execução i
                    if (testeAtual.tecOtimizacao == "EE")
                    {
                        topTenPlanosExec = MetodOtimiz.ExecutaRotOtimizacaoEE();           // Executando rotina de otimização EE na execução i
                    }
                    else if (testeAtual.tecOtimizacao == "AG")
                    {
                        topTenPlanosExec = MetodOtimiz.ExecutaRotOtimizacaoAG();           // Executando rotina de otimização AG na execução i
                    }
                    else if (testeAtual.tecOtimizacao == "NSGA-II")
                    {
                        testeAtual.numExec = i + 1;

                        topTenPlanosExec = MetodOtimiz.ExecutaRotOtimizacaoNSGAII();      // Executando rotina de otimização NSGA-II na execução i
                        listaTodosPlanosNSGA.Add(topTenPlanosExec);                       // Adicionando todos os indivíduos da execução atual a lista
                    }
                    TimeSpan dF = Process.GetCurrentProcess().TotalProcessorTime - dI;    // Fim da contagem de tempo da execução i
                    string tempoGasto = getTimeStr(dF);
                    TemposExecucoes[i] = tempoGasto;
                    impResultadosGeraisExecucao(nomePasta, testeAtual.numTeste, i + 1, topTenPlanosExec, sistema, tempoGasto, sementeAtual);
                    Console.WriteLine(" Numero de avaliacoes de confiabilidade: " + sistema.contAvalConf);
                    Console.WriteLine(" Tempo gasto na execucao: " + tempoGasto);
                    Console.WriteLine(" ");
                    listaTodosTopTenPlanosExec.Add(topTenPlanosExec);      // Salvando o topTen da execução atual
                    nMedioAvalConf += sistema.contAvalConf;
                    nMedioEstaConv += sistema.nEstadosConv / sistema.contAvalConf;
                    nMedioGeracoes += sistema.testeAtual.geracoes;
                    nMedioRepeticoes += sistema.testeAtual.repeticoes;
                    sistema.contAvalConf = 0;          // Zerando contagem de avaliações de confiabilidade
                    sementeAtual++;                    // Atualizando semente para próxima execução da rotina de otimização

                }
                nMedioAvalConf /= testeAtual.nExec;    // Calculando valor final da média de avalições de confiabilidade
                nMedioEstaConv /= testeAtual.nExec;    // Calculando valor final da média de estados para convergência da SMCNS
                nMedioGeracoes /= testeAtual.nExec;    // Calculando valor final da média de gerações/iterações da metaheurística
                nMedioRepeticoes /= testeAtual.nExec;  // Calculando valor final da média de repetições das melhores soluções da metaheurística

                // Valores médios da EE(Lambda+Mi)
                gerMedio /= testeAtual.nExec;
                repMedio /= testeAtual.nExec;

                // -------------------------------------------------------
                // Verificando Topten de todas as execuções do teste
                if(testeAtual.tecOtimizacao == "EE" || testeAtual.tecOtimizacao == "AG")
                {
                    List<Plano> topTenPlanos = new List<Plano>();
                    topTenPlanos.AddRange(listaTodosTopTenPlanosExec[0]);
                    for (int i = 1; i < listaTodosTopTenPlanosExec.Count(); i++)
                    { // Verifica cada lista (de cada execução)
                        for (int j = 0; j < listaTodosTopTenPlanosExec[i].Count(); j++)
                        { // Verifica cada plano da lista atual
                            int a = VerificaPlanoExisteLista(topTenPlanos, listaTodosTopTenPlanosExec[i][j]);
                            if (a < 0)
                            {
                                topTenPlanos.Add(listaTodosTopTenPlanosExec[i][j]);
                            }
                            else
                            {
                                topTenPlanos[a].incidencia++;
                            }
                        }
                    }
                    topTenPlanos = OrdenaListaPlanosAptidao(topTenPlanos);
                    impResultadosFinalTeste(nomePasta, testeAtual.numTeste, topTenPlanos, sistema, testeAtual.nExec, nMedioAvalConf, nMedioEstaConv, TemposExecucoes, nMedioGeracoes, MetodOtimiz);
                }
                else if(testeAtual.tecOtimizacao == "NSGA-II")
                {
                    // Vendo quais os indivíduos não dominados ao juntar os indivíduos das populações de todas as execuções
                    List<Plano> TodosPlanos = new List<Plano>();                 // Lista para receber todos os indivíduos de todas as execuções
                    List<Plano> topPlanos = new List<Plano>();                   // Lista com todos os indivíduos não dominados
                    List<Plano> topTenPlanos = new List<Plano>();                // Lista contendo dez melhores planos para cada objetivo
                    int[] fronteirasDom;                                         // Array para receber as fronteiras de dominância
                    
                    listaTodosPlanosNSGA = VerificaIncidenciaPlano(listaTodosPlanosNSGA);

                    // Loop para adicionar a lista os indivíduos da i-ésima execução
                    foreach(List<Plano> listaPlano in listaTodosPlanosNSGA)
                    {
                        TodosPlanos.AddRange(listaPlano);                        // Adiciona todos os indivíduos a lista criada
                    }

                    fronteirasDom = MetodOtimiz.VerificaDominancia(TodosPlanos); // Utiliza o método criado para classificar a população em fronteiras de dominância
                    
                    // Loop para adicionar os indivíduos não dominados a lista
                    for(int i = 0; i < TodosPlanos.Count; i++)
                    {
                        // Condição para adicionar a população apenas os indivíduos não dominados
                        if(fronteirasDom[i] == 0)
                        {
                            topPlanos.Add(TodosPlanos[i]); // Adiciona o i-ésimo indivíduo a lista com melhores soluções
                        }
                    }

                    topTenPlanos = BestSolutions(TodosPlanos);
                    
                    impResultadosFinalTeste(nomePasta, testeAtual.numTeste, topTenPlanos, sistema, testeAtual.nExec, nMedioAvalConf, nMedioEstaConv, TemposExecucoes, nMedioGeracoes, MetodOtimiz);
                }

            }
            #endregion
        }
        // ------------------------------------------------------
        // Método para contagem de tempo
        # region Método para contagem de tempo
        private static string getTimeStr(TimeSpan diff)
        {
            if (diff.Hours > 0)
                return string.Format("{0}h {1}m {2}s", diff.Hours, diff.Minutes, diff.Seconds);
            else if (diff.Minutes > 0)
                return string.Format("{0}m {1}s", diff.Minutes, diff.Seconds);
            else
                return string.Format("{0}s {1}ms", diff.Seconds, diff.Milliseconds);
        }
        # endregion

        // ------------------------------------------------------
        // Método para impressão de resultados gerais da execução
        # region Método impressão de resultados gerais da execução
        private static void impResultadosGeraisExecucao(string _nomePasta, int _numTeste, int _numExec, List<Plano> _topTenPlanosExec, DadosSistema _sistema, string _tempoGasto, int _sementeAtual)
        {
            // Para as técnicas de otimização EE ou AG
            if (_sistema.testeAtual.tecOtimizacao == "EE" || _sistema.testeAtual.tecOtimizacao == "AG")
            {
                // Criando arquivo
                TextWriter fileResGeraisExec = new StreamWriter(new FileStream(@"" + _nomePasta + "//Teste_ " + _numTeste + "_Exec_ " + _numExec + "_ResGerais.txt", FileMode.Create));
            
                // TopTen encontrado na execução
                int contPlano = 0;
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}      {1}      {2}", "EgCost", "LOLC", "Aptidao");
                foreach (Plano plano in _topTenPlanosExec)
                {
                    contPlano++;
                    fileResGeraisExec.Write("{0:00}", contPlano);
                    for (int i = 0; i < plano.vetorPlano.Length; i++)
                    {
                        fileResGeraisExec.Write("    {0:00}", plano.vetorPlano[i]);
                    }
                    fileResGeraisExec.WriteLine("   {0:000.00000}  {1:000.00000}  {2:000.00000}", plano.custoMedioProd, plano.lolcPlano, plano.aptidao);
                }

                // Informações gerais da execução
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.WriteLine(" Informações gerais da execução");
                fileResGeraisExec.WriteLine(" ==============================");
                fileResGeraisExec.WriteLine(" Sistema teste: {0}", _sistema.testeAtual.sistemaTeste);
                fileResGeraisExec.WriteLine(" Período do ano: {0}", _sistema.testeAtual.periodoEst);
                fileResGeraisExec.WriteLine(" Curva de carga considerada no estudo: {0}", _sistema.testeAtual.curvaDeCarga);
                fileResGeraisExec.WriteLine(" Número do teste: {0}", _sistema.testeAtual.numTeste);
                fileResGeraisExec.WriteLine(" Número de execuções do teste: {0}", _sistema.testeAtual.nExec);
                fileResGeraisExec.WriteLine(" Numero de avaliacoes de confiabilidade: {0:000000}", _sistema.contAvalConf);
                fileResGeraisExec.WriteLine(" Numero médio de estados necessários para convergência da SMCNS: {0:000000}", _sistema.nEstadosConv/_sistema.contAvalConf);
                fileResGeraisExec.WriteLine(" Tempo gasto na execucao: {0}", _tempoGasto);
                fileResGeraisExec.WriteLine(" Semente utilizada: {0}", _sementeAtual);

                // Imprimindo as informações da EE(Lambda+Mi)
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.WriteLine(" Parâmetros utilizados na EE(Lambda+Mi)");
                fileResGeraisExec.WriteLine(" ======================================");
                fileResGeraisExec.WriteLine(" Número de indivíduos por geração: {0}", _sistema.testeAtual.numInd);
                fileResGeraisExec.WriteLine(" Número máximo de gerações: {0}", _sistema.testeAtual.gerMax);
                fileResGeraisExec.WriteLine(" Número máximo de repetições da mehor solução: {0}", _sistema.testeAtual.repMax);
                fileResGeraisExec.WriteLine(" Passo de mutação: {0}", _sistema.testeAtual.sigma * 0.1);
                fileResGeraisExec.WriteLine(" Número de elementos do vetor plano a serem mutados de cada indivíduo de cada geração: {0}", _sistema.testeAtual.numEleMut);
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.WriteLine(" Parâmetros de Saída Referentes a Qualidade da EE(Lambda+Mi)");
                fileResGeraisExec.WriteLine(" ===========================================================");
                fileResGeraisExec.WriteLine(" Número de gerações: {0}", _sistema.testeAtual.geracoes);
                fileResGeraisExec.Close();
            }

            else if(_sistema.testeAtual.tecOtimizacao == "NSGA-II")
            {
                // Criando arquivo
                TextWriter fileResGeraisExec = new StreamWriter(new FileStream(@"" + _nomePasta + "//Teste_ " + _numTeste + "_Exec_ " + _numExec + "_ResGerais.txt", FileMode.Create));
                int numeroSolu = _topTenPlanosExec.Count() - 1;
                double p1 = (numeroSolu) / 4 - 1;
                double p2 = (2 * numeroSolu) / 4 - 1;
                double p3 = (3 * numeroSolu) / 4 - 1;
                double p4 = numeroSolu - 1;
                int parada1 = Convert.ToInt32(p1);
                int parada2 = Convert.ToInt32(p2);
                int parada3 = Convert.ToInt32(p3);
                int parada4 = Convert.ToInt32(p4);

                int contPlano = 0;

                // Dispondo indivíduo do caso base
                fileResGeraisExec.WriteLine("VALORES DAS FUNÇÕES OBJETIVO PARA O CASO BASE");
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                contPlano++;
                fileResGeraisExec.Write("{0:00}", contPlano);
                for (int i = 0; i < _topTenPlanosExec[0].vetorPlano.Length; i++)
                {
                    fileResGeraisExec.Write("    {0:00}", _topTenPlanosExec[numeroSolu].vetorPlano[i]);
                }
                fileResGeraisExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosExec[numeroSolu].custoMedioProd, _topTenPlanosExec[numeroSolu].eensPlano, _topTenPlanosExec[numeroSolu].receitaMediaProdutor);
                
                // TopTen encontrado com melhores valores gerais
                contPlano = 0;
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("DEZ MELHORES SOLUÇÕES PARA TODAS FUNÇÕES OBJETIVO");
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                for (int j = (parada3 + 1); j <= parada4; j ++)
                {
                    contPlano++;
                    fileResGeraisExec.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosExec[0].vetorPlano.Length; i++)
                    {
                        fileResGeraisExec.Write("    {0:00}", _topTenPlanosExec[j].vetorPlano[i]);
                    }
                    fileResGeraisExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosExec[j].custoMedioProd, _topTenPlanosExec[j].eensPlano, _topTenPlanosExec[j].receitaMediaProdutor);
                }

                // TopTen encontrado na execução para o primeiro objetivo
                contPlano = 0;
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("DEZ MELHORES SOLUÇÕES PARA A RECEITA MÉDIA DO PRODUTOR");
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                for (int j = 0; j <= parada1; j ++)
                {
                    contPlano++;
                    fileResGeraisExec.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosExec[0].vetorPlano.Length; i++)
                    {
                        fileResGeraisExec.Write("    {0:00}", _topTenPlanosExec[j].vetorPlano[i]);
                    }
                    fileResGeraisExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosExec[j].custoMedioProd, _topTenPlanosExec[j].eensPlano, _topTenPlanosExec[j].receitaMediaProdutor);
                }

                // TopTen encontrado na execução para o segundo objetivo
                contPlano = 0;
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("DEZ MELHORES SOLUÇÕES PARA O ÍNDICE EENS");
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                for (int j = (parada1 + 1); j <= parada2; j ++)
                {
                    contPlano++;
                    fileResGeraisExec.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosExec[0].vetorPlano.Length; i++)
                    {
                        fileResGeraisExec.Write("    {0:00}", _topTenPlanosExec[j].vetorPlano[i]);
                    }
                    fileResGeraisExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosExec[j].custoMedioProd, _topTenPlanosExec[j].eensPlano, _topTenPlanosExec[j].receitaMediaProdutor);
                }

                // TopTen encontrado na execução para o terceiro objetivo
                contPlano = 0;
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("  ");
                fileResGeraisExec.WriteLine("DEZ MELHORES SOLUÇÕES PARA O CUSTO MÉDIO DE PRODUÇÃO");
                fileResGeraisExec.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("    {0}", "UG"); }
                }
                fileResGeraisExec.WriteLine();
                fileResGeraisExec.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResGeraisExec.Write("   {0:000}", gerador.PotMax); }
                }
                fileResGeraisExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                for (int j = (parada2 + 1); j <= parada3; j ++)
                {
                    contPlano++;
                    fileResGeraisExec.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosExec[0].vetorPlano.Length; i++)
                    {
                        fileResGeraisExec.Write("    {0:00}", _topTenPlanosExec[j].vetorPlano[i]);
                    }
                    fileResGeraisExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosExec[j].custoMedioProd, _topTenPlanosExec[j].eensPlano, _topTenPlanosExec[j].receitaMediaProdutor);
                }

                // Informações gerais da execução
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.WriteLine(" Informações gerais da execução");
                fileResGeraisExec.WriteLine(" ==============================");
                fileResGeraisExec.WriteLine(" Sistema teste: {0}", _sistema.testeAtual.sistemaTeste);
                fileResGeraisExec.WriteLine(" Período do ano: {0}", _sistema.testeAtual.periodoEst);
                fileResGeraisExec.WriteLine(" Curva de carga considerada no estudo: {0}", _sistema.testeAtual.curvaDeCarga);
                fileResGeraisExec.WriteLine(" Número do teste: {0}", _sistema.testeAtual.numTeste);
                fileResGeraisExec.WriteLine(" Número de execuções do teste: {0}", _sistema.testeAtual.nExec);
                fileResGeraisExec.WriteLine(" Numero de avaliacoes de confiabilidade: {0:000000}", _sistema.contAvalConf);
                fileResGeraisExec.WriteLine(" Numero médio de estados necessários para convergência da SMCNS: {0:000000}", _sistema.nEstadosConv/_sistema.contAvalConf);
                fileResGeraisExec.WriteLine(" Tempo gasto na execucao: {0}", _tempoGasto);
                fileResGeraisExec.WriteLine(" Semente utilizada: {0}", _sementeAtual);

                // Imprimindo as informações da NSGA-II
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.WriteLine(" Parâmetros utilizados na NSGA-II");
                fileResGeraisExec.WriteLine(" ======================================");
                fileResGeraisExec.WriteLine(" Número de indivíduos por geração: {0}", _sistema.testeAtual.numInd);
                fileResGeraisExec.WriteLine(" Número máximo de gerações: {0}", _sistema.testeAtual.gerMax);
                fileResGeraisExec.WriteLine(" Número de gerações nessa execução: {0}", _sistema.testeAtual.geracoes);
                fileResGeraisExec.WriteLine(" Número máximo de repetições das melhores soluções: {0}", _sistema.testeAtual.repMax);
                fileResGeraisExec.WriteLine(" Número de repetições nessa execução: {0}", _sistema.testeAtual.repeticoes);
                fileResGeraisExec.WriteLine(" ");
                fileResGeraisExec.Close();
            }
        }
        # endregion

        // ------------------------------------------------------
        // Método para impressão de resultados finais do teste
        # region Método impressão de resultados finais do teste
        private static void impResultadosFinalTeste(string _nomePasta, int _numTeste, List<Plano> _topTenPlanosTeste, DadosSistema _sistema, int _nExec, double _nMedioAvalConf, double _nMedioEstaConv, string[] _TemposExecucoes, double _nMedioGeracoes, RotOtimizacao _MetodOtimiz)
        {
            // Criando arquivo
            TextWriter fileResFinaisTeste = new StreamWriter(new FileStream(@"" + _nomePasta + "//Teste_ " + _numTeste + "_ResdFinais.txt", FileMode.Create));
            double desvioPercentualMedioAcumulado = 0;
            int contPlano = 0;
            
            if(_sistema.testeAtual.tecOtimizacao == "EE" || _sistema.testeAtual.tecOtimizacao == "AG")
            {
                

                // TopTen encontrado no teste
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}      {1}      {2}   {3}   {4}", "EgCost", "LOLC", "Aptidao","Incidencia","DesvPct(%)");
                foreach (Plano plano in _topTenPlanosTeste)
                {
                    contPlano++;
                    fileResFinaisTeste.Write("{0:00}", contPlano);
                    for (int i = 0; i < plano.vetorPlano.Length; i++)
                    {
                        fileResFinaisTeste.Write("    {0:00}", plano.vetorPlano[i]);
                    }
                    double desvioPercentualMelhor = 100 * (plano.aptidao - _topTenPlanosTeste[0].aptidao) / _topTenPlanosTeste[0].aptidao;
                    desvioPercentualMedioAcumulado += desvioPercentualMelhor;
                    fileResFinaisTeste.WriteLine("   {0:000.00000}  {1:000.00000}  {2:000.00000}    {3:0.0000}      {4:00.0000}", plano.custoMedioProd, plano.lolcPlano, plano.aptidao, 100 * plano.incidencia / _nExec, desvioPercentualMelhor);
                    if (contPlano == 10) break;
                }

                // Informações gerais do teste
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Informações gerais do teste");
                fileResFinaisTeste.WriteLine(" ===========================");
                fileResFinaisTeste.WriteLine(" Sistema Teste: {0}", _sistema.testeAtual.sistemaTeste);
                fileResFinaisTeste.WriteLine(" Período do ano: {0}", _sistema.testeAtual.periodoEst);
                fileResFinaisTeste.WriteLine(" Carga pico (total): {0} (MW)", _sistema.cargaTotal);
                fileResFinaisTeste.WriteLine(" Curva de carga considerada no estudo: {0}", _sistema.testeAtual.curvaDeCarga);
                fileResFinaisTeste.WriteLine(" Reserva de geração ativa a ser respeitada: {0} (MW)", _sistema.testeAtual.reservaGer);
                fileResFinaisTeste.WriteLine(" Número de geradores já previamente programados para manunteção: {0}", _sistema.nGerJaProgMnt);
                fileResFinaisTeste.WriteLine(" Número do teste: {0}", _sistema.testeAtual.numTeste);
                fileResFinaisTeste.WriteLine(" Número de execuções do teste: {0}", _sistema.testeAtual.nExec);
            
                // Imprimindo as informações para SMCNS
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros para SMCNS");
                fileResFinaisTeste.WriteLine(" =====================");
                fileResFinaisTeste.WriteLine(" Tolerância para convergência da SMCNS: {0} (%)", _sistema.testeAtual.betaTol * 100);
                fileResFinaisTeste.WriteLine(" Custo do corte de carga para LOLC (UC): {0} ($/MWh)", _sistema.testeAtual.UC);
                if (_sistema.testeAtual.utilizaCE == true)
                {
                    fileResFinaisTeste.WriteLine(" Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade?: SIM");
                }
                else
                {
                    fileResFinaisTeste.WriteLine(" Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade?: NÃO");
                }

                // Imprimindo as informações gerais da EE(Lambda+Mi) 
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros Utilizados na Técnica de Otimização");
                fileResFinaisTeste.WriteLine(" ======================================");
                if (_sistema.testeAtual.tecOtimizacao == "EE")
                {
                    fileResFinaisTeste.WriteLine(" Técnica de otimização empregada: EE");
                    fileResFinaisTeste.WriteLine(" Número de indivíduos por geração: {0}", _sistema.testeAtual.numInd);
                    fileResFinaisTeste.WriteLine(" Número máximo de gerações: {0}", _sistema.testeAtual.gerMax);
                    fileResFinaisTeste.WriteLine(" Número máximo de repetições da melhor solução: {0}", _sistema.testeAtual.repMax);
                    fileResFinaisTeste.WriteLine(" Passo de mutação: {0}", _sistema.testeAtual.sigma * 0.1);
                }
                else if (_sistema.testeAtual.tecOtimizacao == "AG")
                {
                    fileResFinaisTeste.WriteLine(" Técnica de otimização empregada: AG");
                    fileResFinaisTeste.WriteLine(" Número de indivíduos da população: {0}", _sistema.testeAtual.numInd);
                    fileResFinaisTeste.WriteLine(" Número máximo de gerações: {0}", _sistema.testeAtual.gerMax);
                    fileResFinaisTeste.WriteLine(" Número máximo de repetições da melhor solução: {0}", _sistema.testeAtual.repMax);
                    fileResFinaisTeste.WriteLine(" Taxa de cruzamento: {0} (%)", _MetodOtimiz.taxaCruz);
                    fileResFinaisTeste.WriteLine(" Taxa de mutação: {0} (%)", _MetodOtimiz.taxaMut);
                }
                fileResFinaisTeste.WriteLine(" Número de elementos do vetor plano a serem mutados de cada indivíduo de cada geração: {0}", _sistema.testeAtual.numEleMut);
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros de Saída Referentes à Qualidade da EE(Lambda+Mi)");
                fileResFinaisTeste.WriteLine(" ===========================================================");
                fileResFinaisTeste.WriteLine(" Número médio de gerações necessárias: {0}", _sistema.testeAtual);
                fileResFinaisTeste.WriteLine(" Numero médio de avaliacoes de confiabilidade: {0:0000.00}", _nMedioAvalConf);
                fileResFinaisTeste.WriteLine(" Numero médio de estados necessários para convergência da SMCNS: {0:0000000.00}", _nMedioEstaConv);
                fileResFinaisTeste.WriteLine(" Desvio percentual médio em relação à melhor Aptidão {0:00.0000} (%)", desvioPercentualMedioAcumulado / contPlano);
            }
            else if(_sistema.testeAtual.tecOtimizacao == "NSGA-II")
            {
                int numeroSolu = _topTenPlanosTeste.Count() - 1;
                double p1 = (numeroSolu) / 4 - 1;
                double p2 = (2 * numeroSolu) / 4 - 1;
                double p3 = (3 * numeroSolu) / 4 - 1;
                double p4 = numeroSolu - 1;
                int parada1 = Convert.ToInt32(p1);
                int parada2 = Convert.ToInt32(p2);
                int parada3 = Convert.ToInt32(p3);
                int parada4 = Convert.ToInt32(p4);

                // Dispondo indivíduo do caso base
                contPlano = 1;
                fileResFinaisTeste.WriteLine("VALORES DAS FUNÇÕES OBJETIVO PARA O CASO BASE");
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");
                fileResFinaisTeste.Write("{0:00}", contPlano);
                for (int i = 0; i < _topTenPlanosTeste[0].vetorPlano.Length; i++)
                {
                    fileResFinaisTeste.Write("    {0:00}", _topTenPlanosTeste[numeroSolu].vetorPlano[i]);
                }
                fileResFinaisTeste.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", _topTenPlanosTeste[numeroSolu].custoMedioProd, _topTenPlanosTeste[numeroSolu].eensPlano, _topTenPlanosTeste[numeroSolu].receitaMediaProdutor);
                
                

                // TopTen encontrado em todo o teste com melhores valores gerais
                contPlano = 1;
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("DEZ MELHORES SOLUÇÕES PARA TODAS FUNÇÕES OBJETIVO");
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}       {1}       {2}   {3}", "Custo Médio", "EENS", "Receita Média", "Incidência");
                for (int j = (parada3 + 1); j <= parada4; j++)
                {
                    fileResFinaisTeste.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosTeste[0].vetorPlano.Length; i++)
                    {
                        fileResFinaisTeste.Write("    {0:00}", _topTenPlanosTeste[j].vetorPlano[i]);
                    }
                    fileResFinaisTeste.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}     {3:000.00000}", _topTenPlanosTeste[j].custoMedioProd, _topTenPlanosTeste[j].eensPlano, _topTenPlanosTeste[j].receitaMediaProdutor, 100 * (_topTenPlanosTeste[j].incidencia / _sistema.testeAtual.nExec));
                    contPlano++;
                }

                // TopTen encontrado em todo o teste para o primeiro objetivo
                contPlano = 1;
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("DEZ MELHORES SOLUÇÕES PARA A RECEITA DO PRODUTOR");
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}       {1}       {2}   {3}", "Custo Médio", "EENS", "Receita Média", "Incidência");
                for (int j = 0; j <= parada1; j++)
                {
                    fileResFinaisTeste.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosTeste[0].vetorPlano.Length; i++)
                    {
                        fileResFinaisTeste.Write("    {0:00}", _topTenPlanosTeste[j].vetorPlano[i]);
                    }
                    fileResFinaisTeste.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}     {3:000.00000}", _topTenPlanosTeste[j].custoMedioProd, _topTenPlanosTeste[j].eensPlano, _topTenPlanosTeste[j].receitaMediaProdutor, 100 * (_topTenPlanosTeste[j].incidencia / _sistema.testeAtual.nExec));
                    contPlano++;
                }

                // TopTen encontrado em todo o teste para o segundo objetivo
                contPlano = 1;
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("DEZ MELHORES SOLUÇÕES PARA O ÍNDICE EENS");
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}       {1}       {2}   {3}", "Custo Médio", "EENS", "Receita Média", "Incidência");
                for (int j = (parada1 + 1); j <= parada2; j++)
                {
                    fileResFinaisTeste.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosTeste[0].vetorPlano.Length; i++)
                    {
                        fileResFinaisTeste.Write("    {0:00}", _topTenPlanosTeste[j].vetorPlano[i]);
                    }
                    fileResFinaisTeste.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}     {3:000.00000}", _topTenPlanosTeste[j].custoMedioProd, _topTenPlanosTeste[j].eensPlano, _topTenPlanosTeste[j].receitaMediaProdutor, 100 * (_topTenPlanosTeste[j].incidencia / _sistema.testeAtual.nExec));
                    contPlano++;
                }

                // TopTen encontrado em todo o teste para o terceiro objetivo
                contPlano = 1;
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("  ");
                fileResFinaisTeste.WriteLine("DEZ MELHORES SOLUÇÕES PARA O CUSTO DE PRODUÇÃO");
                fileResFinaisTeste.Write("  ");
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("    {0}", "UG"); }
                }
                fileResFinaisTeste.WriteLine();
                fileResFinaisTeste.Write("CM"); // CM: Cronograma de manutenção
                foreach (Gerador gerador in _sistema.Geradores)
                {
                    if (gerador.progMnt == true) { fileResFinaisTeste.Write("   {0:000}", gerador.PotMax); }
                }
                fileResFinaisTeste.WriteLine("    {0}       {1}       {2}   {3}", "Custo Médio", "EENS", "Receita Média", "Incidência");
                for (int j = (parada2 + 1); j <= parada3; j++)
                {
                    fileResFinaisTeste.Write("{0:00}", contPlano);
                    for (int i = 0; i < _topTenPlanosTeste[0].vetorPlano.Length; i++)
                    {
                        fileResFinaisTeste.Write("    {0:00}", _topTenPlanosTeste[j].vetorPlano[i]);
                    }
                    fileResFinaisTeste.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}     {3:000.00000}", _topTenPlanosTeste[j].custoMedioProd, _topTenPlanosTeste[j].eensPlano, _topTenPlanosTeste[j].receitaMediaProdutor, 100 * (_topTenPlanosTeste[j].incidencia / _sistema.testeAtual.nExec));
                    contPlano++;
                }

                // Informações gerais do teste
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Informações gerais do teste");
                fileResFinaisTeste.WriteLine(" ===========================");
                fileResFinaisTeste.WriteLine(" Sistema Teste: {0}", _sistema.testeAtual.sistemaTeste);
                fileResFinaisTeste.WriteLine(" Período do ano: {0}", _sistema.testeAtual.periodoEst);
                fileResFinaisTeste.WriteLine(" Carga pico (total): {0} (MW)", _sistema.cargaTotal);
                fileResFinaisTeste.WriteLine(" Curva de carga considerada no estudo: {0}", _sistema.testeAtual.curvaDeCarga);
                fileResFinaisTeste.WriteLine(" Reserva de geração ativa a ser respeitada: {0} (MW)", _sistema.testeAtual.reservaGer);
                fileResFinaisTeste.WriteLine(" Número de geradores já previamente programados para manunteção: {0}", _sistema.nGerJaProgMnt);
                fileResFinaisTeste.WriteLine(" Número do teste: {0}", _sistema.testeAtual.numTeste);
                fileResFinaisTeste.WriteLine(" Número de execuções do teste: {0}", _sistema.testeAtual.nExec);
            
                // Imprimindo as informações para SMCNS
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros para SMCNS");
                fileResFinaisTeste.WriteLine(" =====================");
                fileResFinaisTeste.WriteLine(" Tolerância para convergência da SMCNS: {0} (%)", _sistema.testeAtual.betaTol * 100);
                fileResFinaisTeste.WriteLine(" Custo do corte de carga para LOLC (UC): {0} ($/MWh)", _sistema.testeAtual.UC);
                if (_sistema.testeAtual.utilizaCE == true)
                {
                    fileResFinaisTeste.WriteLine(" Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade?: SIM");
                }
                else
                {
                    fileResFinaisTeste.WriteLine(" Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade?: NÃO");
                }

                // Imprimindo as informações gerais da EE(Lambda+Mi) 
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros Utilizados na Técnica de Otimização");
                fileResFinaisTeste.WriteLine(" ======================================");

                fileResFinaisTeste.WriteLine(" Técnica de otimização empregada: NSGA-II");
                fileResFinaisTeste.WriteLine(" Número de indivíduos da população: {0}", _sistema.testeAtual.numInd);
                fileResFinaisTeste.WriteLine(" Número máximo de gerações: {0}", _sistema.testeAtual.gerMax);
                fileResFinaisTeste.WriteLine(" Número máximo de repetições da população estagnada: {0}", _sistema.testeAtual.repMax);
                fileResFinaisTeste.WriteLine(" Taxa de cruzamento: {0} (%)", _MetodOtimiz.taxaCruz);
                fileResFinaisTeste.WriteLine(" Taxa de mutação: {0} (%)", _MetodOtimiz.taxaMut);
                fileResFinaisTeste.WriteLine(" Número de elementos do vetor plano a serem mutados de cada indivíduo de cada geração: {0}", _sistema.testeAtual.numEleMut);
                fileResFinaisTeste.WriteLine(" ");
                fileResFinaisTeste.WriteLine(" Parâmetros de Saída Referentes à Qualidade da NSGA-II");
                fileResFinaisTeste.WriteLine(" ===========================================================");
                fileResFinaisTeste.WriteLine(" Número médio de gerações necessárias: {0}", _nMedioGeracoes);
                fileResFinaisTeste.WriteLine(" Numero médio de avaliacoes de confiabilidade: {0:0000.00}", _nMedioAvalConf);
                fileResFinaisTeste.WriteLine(" Numero médio de estados necessários para convergência da SMCNS: {0:0000000.00}", _nMedioEstaConv);
            }
            
            for (int i = 0; i < _nExec; i++)
            {
                fileResFinaisTeste.WriteLine(" Tempo da execução {0:000}: {1}", i + 1, _TemposExecucoes[i]);
            }
            fileResFinaisTeste.Close();
        }
        # endregion

        // ------------------------------------------------
        // Métodos auxiliares da ferramenta de otimização
        # region Métodos de auxílio à ferramenta de otimização

        // ------------------------------------------------
        // Ordena planos em uma lista pela aptidão (devolve lista de planos ordenados pela aptidão)
        private static List<Plano> OrdenaListaPlanosAptidao(List<Plano> _ListaOrdenar)
        {
            List<Plano> ListaOrdenada = new List<Plano>();
            int[] ordem = new int[_ListaOrdenar.Count()];
            double[] aptidao = new double[_ListaOrdenar.Count()];
            for (int i = 0; i < _ListaOrdenar.Count(); i++)
            {
                ordem[i] = i;
                aptidao[i] = _ListaOrdenar[i].aptidao;
            }
            Array.Sort(aptidao, ordem);
            for (int i = 0; i < _ListaOrdenar.Count(); i++)
            {
                ListaOrdenada.Add(_ListaOrdenar[ordem[i]]);
            }
            return ListaOrdenada;
        }

        // ------------------------------------------------
        // Compara planos para verificar se são iguais (se iguais retorna true, se diferentes retorna false)
        private static bool VerificaPlanosIguais(Plano _plano1, Plano _plano2)
        {
            bool iguais = true;
            _plano1.OrdenaPlanoAvalConf();      // Garantindo que semanas de início da manutenção tenham sido ordenadas antes da verificação
            _plano2.OrdenaPlanoAvalConf();      // Garantindo que semanas de início da manutenção tenham sido ordenadas antes da verificação
            for (int i = 0; i < _plano1.vetorPlano.Length; i++)
            {
                if (_plano1.vetorPlano[i] != _plano2.vetorPlano[i])
                { // Pelo menos uma posição do vetor plano é diferente
                    iguais = false;
                    break;
                }
            }
            return iguais;
        }

        // ------------------------------------------------
        // Verifica se plano já existe dentro de uma lista (se existe retorna true, se não existe retorna false)
        private static int VerificaPlanoExisteLista(List<Plano> _listaPlano, Plano _planoComp)
        {
            bool existe = false;
            _planoComp.OrdenaPlanoAvalConf();      // Garantindo que semanas de início da manutenção tenham sido ordenadas antes da verificação

            int posDentroLista = 0;
            foreach (Plano plano in _listaPlano)
            {
                if (VerificaPlanosIguais(plano, _planoComp) == true)
                { // Plano é igual a algum plano da lista
                    existe = true;
                    break;
                }
                posDentroLista++;
            }

            if (existe == false)
            {
                posDentroLista = -1;
            }

            return posDentroLista;
        }

        // ------------------------------------------------
        // Contador de quantas vezes um plano se repete dentro de uma lista
        private static List<List <Plano>> VerificaIncidenciaPlano(List<List <Plano>> _TodosPlanos)
        {
            List<List <Plano>> AuxTodosPlanos = new List<List<Plano>>();
            bool indIguais;
            
            foreach(List<Plano> PlanosExec in _TodosPlanos)
            {
                AuxTodosPlanos.Add(PlanosExec);
            }

            // Verificando incidência dos indivíduos com melhores valores para a receita média do produtor
            for(int i = 0; i < _TodosPlanos.Count(); i++)
            {
                for(int j = 0; j < _TodosPlanos.Count(); j++)
                {
                    if(i != j)
                    {
                        for(int k = 0; k < (_TodosPlanos[i].Count() / 4); k++)
                        {
                            for(int l = 0; l < (_TodosPlanos[j].Count() / 4); l++)
                            {
                                indIguais = true;

                                for(int m = 0; m < _TodosPlanos[i][k].vetorPlano.Length; m++)
                                {
                                    if(_TodosPlanos[i][k].vetorPlano[m] != _TodosPlanos[j][l].vetorPlano[m])
                                    {
                                        indIguais = false;
                                        break;
                                    }
                                }

                                if(indIguais == true)
                                {
                                    AuxTodosPlanos[i][k].incidencia++;
                                }
                            }
                        }
                    }
                }
            }

            // Verificando incidência dos indivíduos com melhores valores para o índice EENS
            for(int i = 0; i < _TodosPlanos.Count(); i++)
            {
                for(int j = 0; j < _TodosPlanos.Count(); j++)
                {
                    if(i != j)
                    {
                        for(int k = (_TodosPlanos[i].Count() / 4); k < 2 * (_TodosPlanos[i].Count() / 4); k++)
                        {
                            for(int l = (_TodosPlanos[j].Count() / 4); l < 2 * (_TodosPlanos[j].Count() / 4); l++)
                            {
                                indIguais = true;

                                for(int m = 0; m < _TodosPlanos[i][k].vetorPlano.Length; m++)
                                {
                                    if(_TodosPlanos[i][k].vetorPlano[m] != _TodosPlanos[j][l].vetorPlano[m])
                                    {
                                        indIguais = false;
                                        break;
                                    }
                                }

                                if(indIguais == true)
                                {
                                    AuxTodosPlanos[i][k].incidencia++;
                                }
                            }
                        }
                    }
                }
            }

            // Verificando incidência dos indivíduos com melhores valores para o custo médio de produção
            for(int i = 0; i < _TodosPlanos.Count(); i++)
            {
                for(int j = 0; j < _TodosPlanos.Count(); j++)
                {
                    if(i != j)
                    {
                        for(int k = 2 * (_TodosPlanos[i].Count() / 4); k < 3 * (_TodosPlanos[i].Count() / 4); k++)
                        {
                            for(int l = 2 * (_TodosPlanos[j].Count() / 4); l < 3 * (_TodosPlanos[j].Count() / 4); l++)
                            {
                                indIguais = true;

                                for(int m = 0; m < _TodosPlanos[i][k].vetorPlano.Length; m++)
                                {
                                    if(_TodosPlanos[i][k].vetorPlano[m] != _TodosPlanos[j][l].vetorPlano[m])
                                    {
                                        indIguais = false;
                                        break;
                                    }
                                }

                                if(indIguais == true)
                                {
                                    AuxTodosPlanos[i][k].incidencia++;
                                }
                            }
                        }
                    }
                }
            }

            // Verificando incidência dos indivíduos com melhores valores gerais
            for(int i = 0; i < _TodosPlanos.Count(); i++)
            {
                for(int j = 0; j < _TodosPlanos.Count(); j++)
                {
                    if(i != j)
                    {
                        for(int k = 3 * (_TodosPlanos[i].Count() / 4); k < 4 * (_TodosPlanos[i].Count() / 4); k++)
                        {
                            for(int l = 3 * (_TodosPlanos[j].Count() / 4); l < 4 * (_TodosPlanos[j].Count() / 4); l++)
                            {
                                indIguais = true;

                                for(int m = 0; m < _TodosPlanos[i][k].vetorPlano.Length; m++)
                                {
                                    if(_TodosPlanos[i][k].vetorPlano[m] != _TodosPlanos[j][l].vetorPlano[m])
                                    {
                                        indIguais = false;
                                        break;
                                    }
                                }

                                if(indIguais == true)
                                {
                                    AuxTodosPlanos[i][k].incidencia++;
                                }
                            }
                        }
                    }
                }
            }

            return AuxTodosPlanos;
        }

        // Método para retorno das 40 melhores soluções de toda execução
        private static List<Plano> BestSolutions(List<Plano> _Populacao)
        {
            // Instanciando variáveis necessárias
            List<Plano> TopSolutions = new List<Plano>();
            List<Plano> AuxPopulacao = new List<Plano>();
            List<int> IndRepetidos = new List<int>();
            double [] valorObj1;
            double [] valorObj2;
            double [] valorObj3;
            double [] volume;
            int [] posicao1;
            int [] posicao2;
            int [] posicao3;
            int [] posicao4;
            double obj1 = new double();
            double obj2 = new double();
            double obj3 = new double();
            int numInd = _Populacao.Count();
            int numGer = _Populacao[0].vetorPlano.Length;
            int numIndCasoBase = new int();
            int contInd;
            bool indIguais;
            

            // Loop para detecção de indivíduos repetidos
            for(int i = 0; i < numInd; i++)
            {
                for(int j = (i + 1); j < numInd; j++)
                {
                    indIguais = true;

                    for(int k = 0; k < numGer; k++)
                    {
                        if(_Populacao[i].vetorPlano[k] != _Populacao[j].vetorPlano[k])
                        {
                            indIguais = false;
                            break;
                        }
                    }

                    if(indIguais == true)
                    {
                        IndRepetidos.Add(j);
                    }
                }
            }

            AuxPopulacao = CopiarPopulacao(_Populacao);

            // Loop para remoção de indivíduos repetidos
            for(int i = 0; i < IndRepetidos.Count(); i++)
            {
                AuxPopulacao.Remove(_Populacao[IndRepetidos[i]]);
            }

            // Detecção do indivíduo de caso base
            for(int i = 0; i < _Populacao.Count(); i++)
            {
                if(_Populacao[i].vetorPlano[0] == 0)
                {
                    numIndCasoBase = i;
                    obj1 = _Populacao[i].receitaMediaProdutor;
                    obj2 = _Populacao[i].eensPlano;
                    obj3 = _Populacao[i].custoMedioProd;
                    break;
                }
            }

            // Remoção do indivíduo de caso base
            AuxPopulacao.Remove(_Populacao[numIndCasoBase]);

            // Instanciando tamanho dos arrays para valor dos objetivos
            valorObj1 = new double[AuxPopulacao.Count()];
            valorObj2 = new double[AuxPopulacao.Count()];
            valorObj3 = new double[AuxPopulacao.Count()];
            volume = new double[AuxPopulacao.Count()];
            posicao1 = new int[AuxPopulacao.Count()];
            posicao2 = new int[AuxPopulacao.Count()];
            posicao3 = new int[AuxPopulacao.Count()];
            posicao4 = new int[AuxPopulacao.Count()];

            // Preenchendo arrays com os seus valores correspondentes
            for(int i = 0; i < AuxPopulacao.Count(); i++)
            {
                valorObj1[i] = AuxPopulacao[i].receitaMediaProdutor;
                valorObj2[i] = AuxPopulacao[i].eensPlano;
                valorObj3[i] = AuxPopulacao[i].custoMedioProd;
                volume[i] = Math.Abs((2 * obj1 - valorObj1[i]) * (valorObj2[i] - obj2) * (valorObj3[i] - obj3));
                posicao1[i] = i;
                posicao2[i] = i;
                posicao3[i] = i;
                posicao4[i] = i;
            }

            // Ordenando arrays para mostrar aqueles com melhores valores para os objetivos
            Array.Sort(valorObj1, posicao1);
            Array.Reverse(posicao1);
            Array.Sort(valorObj2, posicao2);
            Array.Sort(valorObj3, posicao3);
            Array.Sort(volume, posicao4);

            // Adicionando melhores soluções à lista
            for(int i = 0; i < 10; i++)
            {
                TopSolutions.Add(AuxPopulacao[posicao1[i]]);
            }

            for(int i = 0; i < 10; i++)
            {
                TopSolutions.Add(AuxPopulacao[posicao2[i]]);
            }

            for(int i = 0; i < 10; i++)
            {
                TopSolutions.Add(AuxPopulacao[posicao3[i]]);
            }

            for(int i = 0; i < 10; i++)
            {
                TopSolutions.Add(AuxPopulacao[posicao4[i]]);
            }

            TopSolutions.Add(_Populacao[numIndCasoBase]);
            
            // Retornando lista com melhores soluções
            return TopSolutions;
        }

        // Método para copiar populações
        private static List<Plano> CopiarPopulacao(List<Plano> _Populacao)
        {
            List<Plano> PopulacaoCopia = new List<Plano>(); // População auxiliar para receber a população a ser copiada

            // Loop para cópia de cada indivíduo da população
            for (int i = 0; i < _Populacao.Count; i++)
            {
                PopulacaoCopia.Add(_Populacao[i]);          // Adiciona o i-ésimo indivíduo a população copiada
            }

            return PopulacaoCopia;                          // Retorna para a lista a população copiada
        }

        # endregion
    }

    // Classe para armazenar informações (parâmetros) de cada teste
    # region Classe para organização dos testes
    public class Testes
    {
        // ------------------------------------------------
        // Atributos da Classe
        public int numTeste = new int();                   // Número (contador) de testes
        public int semente = new int();                    // Semente do teste (para geração de números aleatórios na ferramenta de otimização)
        public string sistemaTeste;                        // Nome do sistema a ser utilizado no teste
        public string periodoEst;                          // Período (intervalo) de estudo
        public int semIniMnt = new int();                  // Semana inicial do período de manutenção do cada teste
        public int semFimMnt = new int();                  // Semana fim do período de manutenção do cada teste (inclui a própria semana)
        public int nExec = new int();                      // Número de execuções (rodadas) do teste
        public int nSemPerEst = new int();                 // Número de semanas do período de estudo
        public double betaTol;                             // Tolerância para convergência da SMCNS
        public double UC;                                  // Custo do corte de carga ($/MWh)
        public bool utilizaCE;                             // Utiliza Entropia Cruzada (CE) na avaliação de confiabilidade? (true or false)
        public double reservaGer;                          // Reserva de geração ativa a ser considerada no estudo (MW)

        // Parâmetros para técnica de otimização
        public string tecOtimizacao;                      // Técnica de otimização a ser utilizada (EE ou AG)
        public int numInd;                                // Tamanho da população de indivíduos
        public int gerMax;                                // Número máximo de gerações
        public int repMax;                                // Número máximo de repetições p/ a melhor solução
        public double sigma;                              // Passo de mutação (EE)
        public int numEleMut;                             // Número de elementos a serem mutados do vetorPlano de cada indivíduo a cada geração (EE)
        public int geracoes;                              // Número de gerações necessárias para convergência
        public int repeticoes;                            // Número de repetições de cada uma das execuções
        public string curvaDeCarga;                       // Curva de carga a ser considerada no estudo (PICO ou HORA)

        public string nomePasta;                          // Nome do diretório para impressão de resultados
        public int numExec;                               // Número de execução do teste atual
        
        // ------------------------------------------------
        // Método construtor da classe Testes
        public Testes()
        {
        }
    }
    # endregion
}
