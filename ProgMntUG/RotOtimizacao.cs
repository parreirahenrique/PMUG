using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace ProgMntUG
{
    // Classe para implementação a da rotina de otimização
    # region Classe rotina de otimização
    class RotOtimizacao
    {
        // ------------------------------------------------
        // Atributos da Classe
        DadosSistema sistema;            // Sistema teste para estudo
        ConfSMCNS SMCNS;                 // Objeto para SMCNS
        Random rd;                       // Objeto para geração de número aleatório

        // Atributos para AG e NSGA-II
        public double taxaCruz;          // Taxa de cruzamento
        public double taxaMut;           // Taxa de mutação
        
        List<Plano> topTenPlanosExec = new List<Plano>();      // Lista de 10 melhores planos identificados na execução

        // ------------------------------------------------
        // Método construtor da rotina de otimização
        public RotOtimizacao(DadosSistema _sistema, int _sementeAtual)
        {
            this.sistema = _sistema;
            this.SMCNS = new ConfSMCNS(sistema);
            this.rd = new Random(_sementeAtual);

            // Taxa de mutação para técnica de otimização AG
            if(this.sistema.testeAtual.tecOtimizacao == "AG")
            {
                this.taxaCruz = 65;
                this.taxaMut = 8;
            }

            //Taxa de cruzamento para técnica de otimização NSGA-II
            else if(this.sistema.testeAtual.tecOtimizacao == "NSGA-II")
            {
                this.taxaCruz = 65;
                this.taxaMut = 8;
            }

            SMCNS.DistorcaoParametrosCE();
        }

        // ============================================================================================
        // Métodos para execução da rotina de otimização (rotina principal da ferramenta de otimização)
        #region Métodos de Otimização (EE e AG)

        // ------------------------------------------------
        // Solução pelo método de Estratégia de Evolução (EE)
        #region Método da Rotina de Otimização EE
        public List<Plano> ExecutaRotOtimizacaoEE()
        {
            // ------------------------------------------------
            // Criação de um plano para teste manual
            //Plano planoTeste = new Plano(sistema);
            //planoTeste.vetorPlano[0] = 12;
            //planoTeste.vetorPlano[1] = 11;
            //planoTeste.vetorPlano[2] = 12;
            //planoTeste.vetorPlano[3] = 12;
            //planoTeste.vetorPlano[4] = 12;
            //planoTeste.vetorPlano[5] = 12;
            //planoTeste.vetorPlano[6] = 9;
            //planoTeste.vetorPlano[7] = 12;
            //planoTeste.AvaliaPlano(SMCNS);   // Avaliando plano

            // ------------------------------------------------
            // Criação do plano caso base (sem unidades programadas para manutenção)
            Plano planoCasoBase = new Plano(sistema);
            planoCasoBase.AvaliaPlano(SMCNS);   // Avaliando plano

            //// ------------------------------------------------
            //// Calculando bônus para cada gerador que deve entrar em manutenção (para função objetivo)
            //double PotTotGerMnt = 0;                                    // Somatório das capacidades de geração de cada gerador a ter sua manutenção programada
            //List<Gerador> GeradoresMnt = new List<Gerador>();
            //foreach (Gerador gerador in sistema.Geradores) { if (gerador.progMnt == true) { GeradoresMnt.Add(gerador); PotTotGerMnt += gerador.PotMax; } }
            //foreach (Gerador gerador in GeradoresMnt)
            //{ // Dividindo bônus proporcionalmente à capacidade de geração de cada gerador
            //    gerador.bonusGer = (gerador.PotMax / PotTotGerMnt) * planoCasoBase.aptidao;
            //}

            // ======================================================================
            // ESTRATÉGIA DE EVOLUÇÃO - EE
            # region Rotina do EE

            // Parâmetros de entrada da EE(Lambda+Mi)
            int numInd = sistema.testeAtual.numInd;                                             // Número de indíviduos
            int gerMax = sistema.testeAtual.gerMax;                                             // Número máximo de gerações
            int repMax = sistema.testeAtual.repMax;                                             // Número máximo de repretições da melhor solução
            double sigma = sistema.testeAtual.sigma * 0.1;                                      // Passo de mutação
            int numEleMut = sistema.testeAtual.numEleMut;                                       // Número de elemetos a serem mutados do vetorPlano de cada individuo de cada geração
            int semIni;                                                                         // Semana inicial do período teste
            int semFim;                                                                         // Semana final do período teste
            // Caso o periodo de teste for o "ANO"
            if ( sistema.testeAtual.periodoEst == "ANO" )
            {
                semIni = 01;                                                                    // Semana inicial do período teste
                semFim = 52;                                                                    // Semana final do período teste
            }
            // Caso o periodo de teste for o "INV", "VER", "OUT" ou "PRI"
            else
            {
                semIni = 01;                                                                    // Semana inicial do período teste
                semFim = 13;                                                                    // Semana final do período teste
            }

            // População inicial
            #region População inicial EE(Lambda+Mi)
            List<Plano> xInicial = new List<Plano>();                                          // Lista da população inicial
            for (int i = 0; i < numInd; i++)
            {
                Plano plano = new Plano(sistema);                                              // Plano auxliar p/ o processo de geração da população inicial
                for (int j = 0; j < plano.vetorPlano.Length; j++)
                {
                    int semana = Convert.ToInt16(rd.Next(semIni, semFim));                     // Gerando um número aleatório, inteiro entre 0 e "numSem"
                    plano.vetorPlano[j] = semana;                                              // Preenchendo o vetorPlano
                }
                plano.AvaliaPlano(SMCNS);                                                      // Avaliando o plano aleatótio criado
                xInicial.Add(plano);                                                           // Preenchendo a lista da população inicial
            }
            #endregion
            int stop = 1;

            // EE(Lambda+Mi)
            #region EE(Lambida+Mi)
            //
            int iter = 1;                                                                       // Número de iterações
            int rep = 00;                                                                       // Número de repetições
            //
            Plano melhorPlano = new Plano(sistema);                                             // Melhor plano encontrado pela EE(Lambda+Mi) ao longo de sua evolução
            melhorPlano.aptidao = double.MaxValue;                                              // Iniciando a aptidão do melhor plano
            Plano possivelMelhorPlano = new Plano(sistema);                                     // Plano auxiliar p/ determinação do melhor plano

            // Loop de evolução da EE(Lambda+Mi)
            while(iter < gerMax && rep < repMax)
            {
                // Clonagem
                List<Plano> xClone = new List<Plano>();                                         // Lista de clones
                for (int i = 0; i < xInicial.Count; i++)
                {
                    Plano planoCopia = new Plano(sistema);                                      // Plano auxiliar p/ o processo de clonagem
                    planoCopia = CopiaPlano(xInicial[i]);                                       // Copiando o plano na posição "i" de xInicial
                    xClone.Add(planoCopia);                                                     // Preenchendo a lista de clones
                }

                // Mutacao
                List<Plano> xMutado = new List<Plano>();                                        // Lista de indivíduos mutados

                // Percorrendo toda lista xClone
                for (int i = 0; i < xClone.Count; i++)
                {
                    bool planoNovoCriado = false;                                               // Variável auxiliar que garante que um novo plano é criado a cada nova mutação

                    while (planoNovoCriado == false)
                    {
                        Plano plano = new Plano(sistema);                                       // Plano auxiliar p/ o processo de mutação
                        plano = CopiaPlano(xClone[i]);                                          // Copia o plano da lista de xClone p/ mutação mas não o avalia

                        // Mutando "numEleMut" do vetorPlano de cada indivíduo de cada geração
                        for (int j = 0; j < numEleMut; j++)
                        {
                            // Decidindo qual posição do elemento plano.vetorPlano vai ser mutado
                            int posEleMut = Convert.ToInt16(rd.Next(0, plano.vetorPlano.Length));

                            // Decidindo se a mutação vai ser positiva ou negativa
                            double sinal;                                                       // Sinal da mutação (positivo ou negativo)
                            sinal = rd.NextDouble();                                            // Se sinal > 0.5, então sinal = +1, senão, sinal = -1
                            if (sinal > 0.5)
                            {
                                sinal = +1;
                            }
                            else
                            {
                                sinal = -1;
                            }

                            // Decidindo o número de semanas que sera adicionado ou subtraido do vetorPlano na posição "posEleMut"
                            double deltaSem = rd.NextDouble();
                            if (deltaSem > 0 && deltaSem <= 0.33)
                            {
                                deltaSem = 1;
                            }
                            else if (deltaSem > 0.33 && deltaSem <= 0.66)
                            {
                                deltaSem = 2;
                            }
                            else if (deltaSem > 0.66)
                            {
                                deltaSem = 3;
                            }

                            // Mutação em si
                            plano.vetorPlano[posEleMut] = plano.vetorPlano[posEleMut] + Convert.ToInt16(Math.Floor(sigma * deltaSem * sinal));

                            // Verificando os limites de variação da mutação
                            // Limite superior
                            if (plano.vetorPlano[posEleMut] >= semFim)
                            {
                                // Caso o gerador seja colocado p/ manutenção na última semana do período, ele deve ser alocado p/ penúltima semana do período
                                plano.vetorPlano[posEleMut] = semFim - 1;
                            }
                            // Limite inferior
                            if (plano.vetorPlano[posEleMut] < semIni)
                            {
                                // Caso o gerador seja colacado p/ manutenção antes da semana inicial do período, ele deve ser alocado p/ semana inicial
                                plano.vetorPlano[posEleMut] = semIni;
                            }

                        } // for i<numEleMut

                        // Verificando se o plano já existe na lista xClone ou xInicial
                        if (VerificaPlanoExisteLista(xInicial, plano) == false && VerificaPlanoExisteLista(xClone, plano) == false)
                        {
                            // Preenchendo a lista de indivíduos mutados, caso seja um plano novo
                            plano.AvaliaPlano(SMCNS);
                            xMutado.Add(plano);
                            planoNovoCriado = true;
                        }
                        else
                        {
                            // Recomeça o processo de criação de um novo plano, caso o plano criado já exista
                            planoNovoCriado = false;
                        }

                    } // while novoPlanoCriado==false

                } // for i<xClone.Count

                // Concatenação
                List<Plano> xConcatenado = new List<Plano>();
                xConcatenado = xInicial.Concat(xMutado).ToList();

                // Ordenação
                xConcatenado = OrdenaListaPlanosAptidao(xConcatenado);

                // Seleção
                xInicial.Clear();
                for (int i = 0; i < numInd; i++)
                {
                    xInicial.Add(xConcatenado[i]);
                }
                
                // Seleção do melhor plano de todas as gerações
                possivelMelhorPlano = xInicial[0];
                if (possivelMelhorPlano.aptidao < melhorPlano.aptidao)
                {
                    melhorPlano = possivelMelhorPlano;
                    rep = 0;
                }
                else
                {
                    rep++;
                }

                // Resetando as variaveis de interesse
                xClone.Clear();
                xMutado.Clear();
                xConcatenado.Clear();

                // Contador de iterações (gerações)
                iter++;
            }
            #endregion
            stop = 1;

            // Preenchendo os 10 melhores planos
            topTenPlanosExec.Clear();
            xInicial = OrdenaListaPlanosAptidao(xInicial);
            for (int i = 0; i < 10; i++)
            {
                if (VerificaPlanoExisteLista(topTenPlanosExec, xInicial[i]) == false)
                {
                    topTenPlanosExec.Add(xInicial[i]);
                }
            }

            sistema.testeAtual.geracoes = iter;
            // ======================================================================
            #endregion

            // Retornando os 10 melhores planos encontrados
            return topTenPlanosExec;
        }
        # endregion

        // ------------------------------------------------
        // Solução pelo método de Algoritmo Genético (AG)
        # region Método da Rotina de Otimização AG
        public List<Plano> ExecutaRotOtimizacaoAG()
        {
            // ------------------------------------------------
            // Criação de um plano para teste manual
            //Plano planoTeste = new Plano(sistema);
            //planoTeste.vetorPlano[0] = 12;
            //planoTeste.vetorPlano[1] = 11;
            //planoTeste.vetorPlano[2] = 12;
            //planoTeste.vetorPlano[3] = 12;
            //planoTeste.vetorPlano[4] = 12;
            //planoTeste.vetorPlano[5] = 12;
            //planoTeste.vetorPlano[6] = 9;
            //planoTeste.vetorPlano[7] = 12;
            //planoTeste.AvaliaPlano(SMCNS);   // Avaliando plano

            // ------------------------------------------------
            // Criação do plano caso base (sem unidades programadas para manutenção)
            Plano planoCasoBase = new Plano(sistema);
            planoCasoBase.AvaliaPlano(SMCNS);   // Avaliando plano

            // ------------------------------------------------
            // Calculando bônus para cada gerador que deve entrar em manutenção (para função objetivo)
            double PotTotGerMnt = 0;                                    // Somatório das capacidades de geração de cada gerador a ter sua manutenção programada
            List<Gerador> GeradoresMnt = new List<Gerador>();
            foreach (Gerador gerador in sistema.Geradores) { if (gerador.progMnt == true) { GeradoresMnt.Add(gerador); PotTotGerMnt += gerador.PotMax; } }
            foreach (Gerador gerador in GeradoresMnt)
            { // Dividindo bônus proporcionalmente à capacidade de geração de cada gerador
                gerador.bonusGer = (gerador.PotMax / PotTotGerMnt) * planoCasoBase.aptidao;
            }

            // ======================================================================
            // ALGORITMO GENÉTICO - AG
            # region Rotina do AG

            int popSize = sistema.testeAtual.numInd;          // Tamanho da população de indivíduos
            int np = sistema.testeAtual.repMax;               // Critério de convergência - Estagnação da melhor solução por np gerações
            this.taxaCruz = 65;                               // Taxa de cruzamento
            this.taxaMut = 8;                                 // Taxa de mutação

            int contGer = 0;                                  // Contador de gerações
            int contGerMelhorInd = 0;                         // Contador de gerações com permanência (estagnação) do melhor indivíduo
            Plano melhorInd = new Plano(sistema);             // Melhor indivíduo já visitado

            // ------------------------------------------------
            // POPULAÇÃO INICIAL DO AG
            # region População Inicial do AG
            List<Plano> PopInicial = new List<Plano>();             // População de indivíduos inicial

            // Porção inteligente (Metade da população)
            int nAmostra = Convert.ToInt16(Math.Floor(0.25 * (sistema.semFimMnt - sistema.semIniMnt + 1)));

            while (PopInicial.Count() < 0.5 * popSize)
            {
                Plano planoIntel = new Plano(sistema);
                int[] semMenorSomLoleSem = new int[nAmostra];           // Vetor com as semanas com menores valores de lole semanal somados
                double[] somLoleSem = new double[nAmostra];             // Vetor com a soma da lole para o intervalo de semanas necessárias para manutenção do gerador
                for (int i = 0; i < somLoleSem.Count(); i++) { somLoleSem[i] = double.MaxValue; }
                foreach (Gerador gerador in sistema.GeradoresMntOrdCapGer)
                {
                    int semana = 0;
                    if (gerador.PotMax >= 100)
                    {
                        for (int i = 0; i < ((sistema.semFimMnt - sistema.semIniMnt + 1) - gerador.nSemNecMnt + 1); i++)
                        {
                            double somAux = 0;
                            for (int j = i; j < i + gerador.nSemNecMnt; j++)
                            {
                                somAux += planoCasoBase.lolePlanoSemanal[j];
                            }
                            if (somAux < somLoleSem[somLoleSem.Count() - 1])
                            {
                                somLoleSem[somLoleSem.Count() - 1] = somAux;
                                semMenorSomLoleSem[somLoleSem.Count() - 1] = i + 1;
                                Array.Sort(somLoleSem, semMenorSomLoleSem);
                            }
                        }
                        // Sorteio da semana entre as selecionadas
                        bool semanaMaxGer = true;
                        while (semanaMaxGer == true)
                        {
                            semana = semMenorSomLoleSem[Convert.ToInt16(Math.Floor(rd.NextDouble() * (semMenorSomLoleSem.Count() - 1 + 0.99)))];
                            int contSem = 0;
                            for (int k = 0; k < planoIntel.vetorPlano.Count(); k++)
                            {
                                int semMin = semana - gerador.nSemNecMnt + 1;
                                int semMax = semana + gerador.nSemNecMnt - 1;
                                if (planoIntel.vetorPlano[k] >= semMin & planoIntel.vetorPlano[k] <= semMax) { contSem++; }
                            }
                            if (contSem <= 2) { semanaMaxGer = false; }
                        }
                    }
                    else
                    {
                        bool semanaMaxGer = true;
                        while (semanaMaxGer == true)
                        {
                            semana = Convert.ToInt16(Math.Floor(1 + rd.NextDouble() * (sistema.semFimMnt - sistema.semIniMnt - 1)));
                            int contSem = 0;
                            for (int k = 0; k < planoIntel.vetorPlano.Count(); k++)
                            {
                                int semMin = semana - gerador.nSemNecMnt + 1;
                                int semMax = semana + gerador.nSemNecMnt - 1;
                                if (planoIntel.vetorPlano[k] >= semMin & planoIntel.vetorPlano[k] <= semMax) { contSem++; }
                            }
                            if (contSem <= 2) { semanaMaxGer = false; }
                        }
                    }
                    planoIntel.vetorPlano[gerador.posvetorPlano] = semana;
                }
                if (VerificaPlanoExisteLista(PopInicial, planoIntel) == false)
                {
                    planoIntel.AvaliaPlano(SMCNS);
                    PopInicial.Add(planoIntel);
                }
            }

            // Porção aleatória
            while (PopInicial.Count() < popSize)
            {
                Plano planoAleat = new Plano(sistema);
                for (int j = 0; j < planoAleat.vetorPlano.Length; j++)
                {
                    int semana = Convert.ToInt16(Math.Floor(1 + rd.NextDouble() * (sistema.semFimMnt - sistema.semIniMnt - 1)));
                    planoAleat.vetorPlano[j] = semana;
                }
                planoAleat.AvaliaPlano(SMCNS);
                if (VerificaPlanoExisteLista(PopInicial, planoAleat) == false)
                {
                    PopInicial.Add(planoAleat);
                }
            }
            #endregion

            PopInicial = OrdenaListaPlanosAptidao(PopInicial);
            melhorInd = PopInicial[0];
            contGerMelhorInd++;
            // Console.WriteLine("Ger: {0:00} - MelhorApt: {1:0.0000} - Rep: {2:00} - EGcost: {3:00.00000} - LOLC: {4:0.00000}", contGer, melhorInd.aptidao, contGerMelhorInd, melhorInd.custoMedioProd, melhorInd.lolcPlano);

            // Guardando indivíduos da população inicial (10 mais atrativos) na lista dos 10 melhores já visitados
            int contIndPopIni = 0;
            while (topTenPlanosExec.Count() < 10 && contIndPopIni < PopInicial.Count())
            {
                topTenPlanosExec.Add(PopInicial[contIndPopIni]);
                contIndPopIni++;
            }

            // ------------------------------------------------
            // GERAÇÕES DO AG
            #region Gerações/iterações do AG
            List<Plano> PopAtual = new List<Plano>();               // População de indivíduos inicial    
            PopAtual.AddRange(PopInicial);
            List<Plano> novaPop = new List<Plano>();                // População descendente

            while (contGerMelhorInd < np)
            {
                contGer++;
                novaPop.Clear();

                double[] roleta = CriaRoleta(PopAtual);             // Roleta para cruzamento
                while (novaPop.Count < PopAtual.Count())
                {
                    // -----------------------------
                    // OPERADOR CRUZAMENTO
                    // -----------------------------
                    // Selecionando dois pais
                    double x1 = rd.NextDouble();                    // Sorteio para escolha do Pai 1
                    double x2 = rd.NextDouble();                    // Sorteio para escolha do Pai 2
                    Plano planoPai1 = new Plano(sistema);           // Pai 1
                    Plano planoPai2 = new Plano(sistema);           // Pai 2
                    bool x1Encontrado = false;
                    bool x2Encontrado = false;
                    for (int i = 1; i < roleta.Count(); i++)
                    {
                        if (roleta[i] > x1 && x1Encontrado == false) { planoPai1 = PopAtual[i]; x1Encontrado = true; }
                        if (roleta[i] > x2 && x2Encontrado == false) { planoPai2 = PopAtual[i]; x2Encontrado = true; }
                        if (x1Encontrado == true && x2Encontrado == true) break;
                    }
                    if (VerificaPlanosIguais(planoPai1, planoPai2) == true) continue;      // Se pais 1 e 2 são o mesmo indivíduo, segue para selecionar novos pais

                    // -----------------------------
                    // Criando dois filhos
                    List<Plano> PlanosFilhos = CruzamentoUniforme(planoPai1, planoPai2);       // Operador de cruzamento uniforme
                    Plano planoFilho1 = PlanosFilhos[0];            // Filho 1
                    Plano planoFilho2 = PlanosFilhos[1];            // Filho 2

                    // -----------------------------
                    // OPERADOR MUTAÇÃO
                    // -----------------------------
                    // Executa operador de mutação
                    planoFilho1 = Mutacao(planoFilho1);
                    planoFilho2 = Mutacao(planoFilho2);

                    if (VerificaPlanoExisteLista(novaPop, planoFilho1) == false && VerificaPlanoExisteLista(PopAtual, planoFilho1) == false)
                    { // Plano não repetido, portanto, é avaliado e armazenado
                        planoFilho1.AvaliaPlano(SMCNS);
                        novaPop.Add(planoFilho1);
                    }
                    if (VerificaPlanoExisteLista(novaPop, planoFilho2) == false && VerificaPlanoExisteLista(PopAtual, planoFilho2) == false)
                    { // Plano não repetido, portanto, é avaliado e armazenado
                        planoFilho2.AvaliaPlano(SMCNS);
                        novaPop.Add(planoFilho2);
                    }
                }

                // -----------------------------
                // SELEÇÃO DOS INDIVÍDUOS QUE SEGUEM PELO PROCESSO EVOLUTIVO

                novaPop.AddRange(PopAtual);
                novaPop = OrdenaListaPlanosAptidao(novaPop);
                PopAtual.Clear();
                int contInd = 0;
                while (PopAtual.Count() < popSize)
                {
                    PopAtual.Add(novaPop[contInd]);

                    if (VerificaPlanoExisteLista(topTenPlanosExec, novaPop[contInd]) == false && novaPop[contInd].aptidao < topTenPlanosExec[topTenPlanosExec.Count() - 1].aptidao)
                    { // Atualizando lista dos 10 melhores indivíduos já visitados
                        topTenPlanosExec.RemoveAt(topTenPlanosExec.Count() - 1);
                        topTenPlanosExec.Add(novaPop[contInd]);
                        topTenPlanosExec = OrdenaListaPlanosAptidao(topTenPlanosExec);
                    }

                    contInd++;
                }
                
                // -----------------------------
                // VERIFICAÇÃO DA CONVERGÊNCIA
                if (PopAtual[0].aptidao < melhorInd.aptidao)
                {
                    melhorInd = PopAtual[0];
                    contGerMelhorInd = 1;
                }
                else { contGerMelhorInd++; }

                // Console.WriteLine("Ger: {0:00} - MelhorApt: {1:0.0000} - Rep: {2:00} - EGcost: {3:00.00000} - LOLC: {4:0.00000}", contGer, melhorInd.aptidao, contGerMelhorInd, melhorInd.custoMedioProd, melhorInd.lolcPlano);
            }
            #endregion

            #endregion
            // ======================================================================

            // Retornando os 10 melhores planos encontrados
            return topTenPlanosExec;
        }
        #endregion

        #endregion
        // ============================================================================================

        // ============================================================================================
        // Método para execução da rotina de otimização multi-objetivo NSGA-II
        #region Método de Otimização Multi-Objetivo NSGA-II
        
        public List<Plano> ExecutaRotOtimizacaoNSGAII()
        {

            // Instanciando parâmetros necessários para a NSGA-II
            int numInd = this.sistema.testeAtual.numInd;          // Número de indivíduos da população
            int gerMax = this.sistema.testeAtual.gerMax;          // Número máximo de gerações
            int repMax = this.sistema.testeAtual.repMax;          // Número máximo de repetições
            int numEleMut = this.sistema.testeAtual.numEleMut;    // Número de elementos do array a serem mutados
            int semIni = this.sistema.testeAtual.semIniMnt;       // Semana inicial do período de estudo
            int semFim = this.sistema.testeAtual.semFimMnt;       // Semana final de período de estudo
            int numTeste = this.sistema.testeAtual.numTeste;      // Variável para o número do teste atual
            int numExec = this.sistema.testeAtual.numExec;        // Variável para o número da execução do teste atual
            string perEst = this.sistema.testeAtual.periodoEst;   // Variável para o período de estudo
            string nomePasta = this.sistema.testeAtual.nomePasta; // Variável com o diretório atual para impressão dos resultados de cada geração
            List<Gerador> UGsMnt = this.sistema.GeradoresMnt;     // Lista com as unidades geradoras do sistema
            Plano indCasoBase = new Plano(sistema);               // Objeto para conter o indivíduo do caso base
            
            double tol = 0.005;                                   // Tolerância para estagnação dos resultados (0,05%)
            double crossRate = this.taxaCruz/100;                 // Variável para taxa de cruzamento
            double mutRate = this.taxaMut/100;                    // Variável para taxa de mutação

            bool planoExistente;                                  // Variável booleana para determinar se um plano existente foi gerado
            bool semanasIguais;                                   // Variável booleana para determinar se há geradores programados para manutenção dentro de um mesmo intervalo dentro de uma usina
            
            // Criando objeto para escrever em um arquivo contendo todos os indivíduos de todas as gerações da execução atual
            TextWriter fileIndExec = new StreamWriter(new FileStream(@"" + nomePasta + "//Teste_ " + numTeste + "_Exec_ " + numExec + "_IndivExec.txt", FileMode.Create));
            TextWriter fileMedRec = new StreamWriter(new FileStream(@"" + nomePasta + "//Teste_ " + numTeste + "_Exec_ " + numExec + "_MedReceita.txt", FileMode.Create));
            TextWriter fileMedEENS = new StreamWriter(new FileStream(@"" + nomePasta + "//Teste_ " + numTeste + "_Exec_ " + numExec + "_MedEENS.txt", FileMode.Create));
            TextWriter fileMedCus = new StreamWriter(new FileStream(@"" + nomePasta + "//Teste_ " + numTeste + "_Exec_ " + numExec + "_MedCusto.txt", FileMode.Create));
            
            // Gerando os indivíduos da população inicial
            #region Compondo população inicial
            
            // Instanciando algumas variáveis necessárias
            List<Plano> PopInicial = new List<Plano>();                                      // Lista com os planos de manutenção (indivíduos) da geração inicial
            int[] arrayUsinas = new int[UGsMnt.Count()];                                     // Array contendo número das usinas das UGs a serem programdas para manutenção
            int[] arrayNSemNecMnt = new int[UGsMnt.Count()];                                 // Array contendo número de semanas que cada gerador necessita para manutenção
            int semMax;                                                                      // Variável para receber a semana máxima em que um gerador pode receber manutenção
            
            // Estimando valores dos objetivos do indivíduo de caso base
            indCasoBase.AvaliaPlano(SMCNS);

            // Armazenando valores dos arrays contendo números das usinas e número de semanas necessários a manutenção
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                arrayUsinas[i] = UGsMnt[i].nUsina;                                           // Armazena número da usina do i-ésimo gerador
                arrayNSemNecMnt[i] = UGsMnt[i].nSemNecMnt;                                   // Armazena número de semanas necessário para manutenção do i-ésimo gerador           
            }

            // Loop para geração dos indivíduos da primeira geração
            for (int i = 0; i < numInd; i++)
            {
                planoExistente = true;                                                       // Reiniciando variável booleana
                semanasIguais = true;                                                        // Reiniciando variável booleana
                
                // Loop será mantido enquanto um plano diferente dos existentes na lista não seja criado não seja criado
                while(planoExistente == true || semanasIguais == true)
                {
                    Plano plano = new Plano(sistema);                                        // Reiniciando plano para recebimento do indivíduo criado para a população inicial

                    // Loop para sorteio de semanas para os genes do indivíduo atual
                    for (int j = 0; j < plano.vetorPlano.Length; j++)
                    {
                        semMax = semFim - UGsMnt[j].nSemNecMnt + 1;
                        int semana = rd.Next(semIni, semMax);                                // Sorteado uma semana aleatória entre as semanas inicial e final
                        plano.vetorPlano[j] = semana;                                        // O número da semana sorteado é armazenado na posição atual do array
                    }
                       
                    plano.AvaliaPlano(SMCNS);                                                // Avalia o plano, e calcula o seu valor para as três funções objetivo
                    planoExistente = VerificaPlanoExisteLista(PopInicial, plano);            // Usando método para verificar se o plano existe na população inicial
                    semanasIguais = VerificaCronograma(plano, arrayUsinas, arrayNSemNecMnt); // Usando método para verificar se geradores não estão sendo colocados para manutenção em um mesmo intervalo para uma mesma usina
                    
                    // Condição para adicionar o plano criado a população inicial
                    if(planoExistente == false && semanasIguais == false)
                    {
                        PopInicial.Add(plano);                                               // Adiciona o plano criado à população inicial
                    }
                }
            }
                
            #endregion
            
            // Classificação dos indivíduos da população inicial em fronteiras de dominância
            #region Classificação da população entre fronteiras de dominância

            int comprimentoPopulacao = 1;                           // Variável para determinar se ainda há indivíduos para classificar
            List<Plano> PopulacaoDom;                               // População auxiliar para os indivíduos dominados da iteração atual
            List<Plano> PopulacaoDomAnt;                            // População auxiliar para on indivíduos dominados da iteração anterior
            List<Plano> PopulacaoClass = new List<Plano>();         // População classificada pelas fronteiras de dominância (da não dominada para mais dominada)
            int[] ArrayFrontDom = new int[numInd];                  // Array contendo as posições do último indivíduo de cada fronteira
            int[] FronteiraNaoDom;                                  // Array contendo quantos indivíduos dominam um indivíduo
            int contPosicoes = 0;

            PopulacaoDom = CopiarPopulacao(PopInicial);             // Copiando a população inicial para a população auxiliar
            

            // Início das iterações para determinar as fronteiras de dominância
            while(comprimentoPopulacao > 0)
            {
                FronteiraNaoDom = VerificaDominancia(PopulacaoDom); // Classifica os indivíduos da população auxiliar
                PopulacaoDomAnt = CopiarPopulacao(PopulacaoDom);    // Armazena a população que já foi classificada
                PopulacaoDom.Clear();                               // Limpa a população atual para receber a próxima população a ser classificada

                // Loop para armazenar os indivíduos dominados e não dominados em suas devidas populações
                for(int i = 0; i < PopulacaoDomAnt.Count; i++)
                {
                    // Apenas os indivíduos não dominados da iteração atual são adicionados a população classificada
                    if(FronteiraNaoDom[i] == 0)
                    {
                        PopulacaoClass.Add(PopulacaoDomAnt[i]);     // Caso o indivíduo seja não dominado dentro da população atual, é adicionado a população classificada
                    }

                    // Apenas os indivíduos dominados são salvos na população para serem classificados
                    if(FronteiraNaoDom[i] != 0)
                    {
                        PopulacaoDom.Add(PopulacaoDomAnt[i]);       // Caso o indivíduo seja dominado dentro da população atual, é adicionado na população que será classificada na próxima iteração
                    }
                }

                comprimentoPopulacao = PopulacaoDom.Count();        // Determinando se ainda há indivíduos que necessitam ser classificados
                PopulacaoDomAnt.Clear();                            // Limpa a população anterior para a próxima iteração

                ArrayFrontDom[contPosicoes] = PopulacaoClass.Count; // Armazena no array a posição do último indivíduo da fronteira atual
                contPosicoes++;                                     // Aumenta o contador de posições ocupadas do array
            }
            
            // Eliminando posições com valores iguais a zero do array
            int nullElem = new int();            // Variável para salvar a posição do array com o último elemento diferente de zero

            // Loop para determinação da posição com o primeiro elemento nulo
            for(int i = (ArrayFrontDom.Length - 1); i >= 0 ; i--)
            {
                if(ArrayFrontDom[i] == 0)
                {
                    nullElem = i;                // Armazena a posição do array com o último elemento diferente de zero
                }
            }

            int [] FrontDom = new int[nullElem]; // Declara novo array com o número de posições necessárias

            for(int i = 0; i < nullElem; i++)
            {
                FrontDom[i] = ArrayFrontDom[i];  // Armazena apenas as posições com valores diferente de zero no novo array
            }
            #endregion

            // Cálculo de Crowding Distance
            #region Cálculo de Crowding Distance
            
            List<Plano> FronteiraAtual = new List<Plano>();      // População com os indivíduos da fronteira de dominância atual
            double [] crowdingDistance = new double [numInd];    // Array para armazenar crowding distance de todos os indivíduos
            double [] auxCrowDist;                               // Array auxiliar para calcular crowding distance
            int numEle;                                          // Variável para o número de indivíduos da fronteira atual
            int contInd = 0;                                     // Contador para a posição do indivíduo dentro de toda a população
            int contPos = 0;                                     // Contador de posições preenchidas do array crowdingDistance

            // Loop para varrer o vetor com as fronteiras de dominância e criar as populações de cada uma das fronteiras
            for(int i = 0; i < nullElem; i++)
            {
                // Loop para adicionar a fronteira os indivíduos pertencentes a ela
                while(contInd < FrontDom[i])
                {
                    FronteiraAtual.Add(PopulacaoClass[contInd]); // Adiciona na população da fronteira atual os indivíduos pertencentes a ela
                    contInd++;                                   // Acresce uma unidade ao contador
                }

                numEle = FronteiraAtual.Count;                   // Variável para determinar o número de elementos da fronteira atual
                auxCrowDist = new double [numEle];               // Array auxiliar contendo o mesmo número de elementos que a fronteira atual

                auxCrowDist = CrowdingDistance(FronteiraAtual);  // Calcula a crowding distance para os indivíduos da fronteira atual

                // Armazenando os dados do array auxiliar no array principal
                for(int k = 0; k < numEle; k++)
                {
                    crowdingDistance[contPos] = auxCrowDist[k];  // Armazena os dados do array auxiliar em seu lugar correspondente
                    contPos++;                                   // Aumenta uma unidade no contador de posições      
                }

                FronteiraAtual.Clear();                          // Limpando os indivíduos da fronteira atual da lista para receber a próxima fronteira
            }
            #endregion

            // Geração da população de filhos
            #region Geração da população de filhos
            
            // Instanciando algumas variáveis necessárias
            List<Plano> PopulacaoFilhos = new List<Plano>();                               // População contendo os filhos da geração atual
            Plano pai1 = new Plano(sistema);                                               // Objeto para receber o primeiro pai
            Plano pai2 = new Plano(sistema);                                               // Objeto para receber o segundo pai
            Plano filho1;                                                                  // Objeto para receber o primeiro filho gerado pelo cruzamento dos pais
            Plano filho2;                                                                  // Objeto para receber o segundo filho gerado pelo cruzamento dos pais
            double numSort;                                                                // Variável para receber um número aleatório sorteado para comparação com a probabilidade de cruzamento
            int genesSort;                                                                 // Variável para sorteio dos genes a serem copiados
            int numGenes = pai1.vetorPlano.Length;                                         // Variável para armazenar o número de genes
            int contIndPop = new int();                                                    // Contador de indivíduos da população de filhos
            bool planoExistente1;                                                          // Variável booleana que diz se o primeiro filho gerado existe na população de pais
            bool planoExistente2;                                                          // Variável booleana que diz se o segundo filho gerado existe na população de pais
            bool planoExistente3;                                                          // Variável booleana que diz se o primeiro filho gerado existe na população de filhos
            bool planoExistente4;                                                          // Variável booleana que diz se o segundo filho gerado existe na população de filhos
            bool semanasIguais1;                                                           // Variável booleana para determinar se há geradores programados para manutenção dentro de um mesmo intervalo dentro de uma usina para o primeiro filho
            bool semanasIguais2;                                                           // Variável booleana para determinar se há geradores programados para manutenção dentro de um mesmo intervalo dentro de uma usina para o segundo filho

            // Loop para cruzar os indivíduos da população
            while(contIndPop < numInd)
            {
                // Condição para adicionar apenas indivíduos não existentes
                pai1 = SortearIndividuos(PopulacaoClass, FrontDom, crowdingDistance);      // Sorteando primeiro pai
                pai2 = SortearIndividuos(PopulacaoClass, FrontDom, crowdingDistance);      // Sorteando segundo pai
                filho1 = new Plano(sistema);                                               // Reiniciando objeto para recebimento do primeiro filho
                filho2 = new Plano(sistema);                                               // Reiniciando objeto para recebimento do segundo filho

                genesSort = rd.Next(0, (numGenes - 1));                                    // Sorteia até qual gene as cópias devem ser feitas
                numSort = rd.NextDouble();                                                 // Sorteia um número para decidir de os pais devem ou não se cruzar
                   
                // Armazenando os genes dos pais nos filhos
                for(int j = 0; j < numGenes; j++)
                {
                    // Armazenando os genes até o gene sorteado
                    if(j <= genesSort)
                    {
                        filho1.vetorPlano[j] = pai1.vetorPlano[j];                         // Primeiro filho recebe os genes do primeiro pai
                        filho2.vetorPlano[j] = pai2.vetorPlano[j];                         // Segundo filho recebe os genes do segundo pai
                    }
                    
                    // Armazenando os genes depois do gene sorteado
                    if(j > genesSort)
                    {
                        filho1.vetorPlano[j] = pai2.vetorPlano[j];                         // Primeiro filho recebe os genes do segundo pai
                        filho2.vetorPlano[j] = pai1.vetorPlano[j];                         // Segundo filho recebe os genes do primeiro pai
                    }
                }

                filho1 = MutacaoIndividuo(filho1);                                         // Usando método para mutar o primeiro indivíduo criado do cruzamento
                filho2 = MutacaoIndividuo(filho2);                                         // Usando método para mutar o segundo indivíduo criado do cruzamento                                          
                
                filho1.AvaliaPlano(SMCNS);                                                 // Avalia o plano gerado através do método
                filho2.AvaliaPlano(SMCNS);                                                 // Avalia o plano gerado através do método
                    
                planoExistente1 = VerificaPlanoExisteLista(PopulacaoClass, filho1);        // Usando método para ver se o indivíduo gerado não existe na população de pais
                planoExistente2 = VerificaPlanoExisteLista(PopulacaoClass, filho2);        // Usando método para ver se o indivíduo gerado não existe na população de pais
                planoExistente3 = VerificaPlanoExisteLista(PopulacaoFilhos, filho1);       // Usando método para ver se o indivíduo gerado não existe na população de filhos
                planoExistente4 = VerificaPlanoExisteLista(PopulacaoFilhos, filho2);       // Usando método para ver se o indivíduo gerado não existe na população de filhos

                semanasIguais1 = VerificaCronograma(filho1, arrayUsinas, arrayNSemNecMnt); // Usando método para ver se o primeiro indivíduo gerado não possui geradores da mesma usina programados para manutenção em um mesmo intervalo
                semanasIguais2 = VerificaCronograma(filho2, arrayUsinas, arrayNSemNecMnt); // Usando método para ver se o segundo indivíduo gerado não possui geradores da mesma usina programados para manutenção em um mesmo intervalo
                
                // Adicionando apenas os filhos que não existam nas populações, desde que o número sorteado seja menor que a taxa de cruzamento
                if(numSort <= crossRate && planoExistente1 == false && planoExistente2 == false && planoExistente3 == false && planoExistente4 == false && semanasIguais1 == false && semanasIguais2 == false)
                {
                    PopulacaoFilhos.Add(filho1);                                           // Adiciona o indivíduo gerado a população de filhos
                    PopulacaoFilhos.Add(filho2);                                           // Adiciona o indivíduo gerado a população de filhos
                    contIndPop = PopulacaoFilhos.Count();                                  // Armazenando a quantidade de indivíduos na população de filhos
                }

                pai1 = new Plano(sistema);                                                 // Reiniciando objeto para recebimento do primeiro pai
                pai2 = new Plano(sistema);                                                 // Reiniciando objeto para recebimento do segundo pai
            }

            #endregion

            // Início das iterações do algortimo NSGA-II
            #region Início das iterações do algoritmo NSGA-II

            // Instanciando algumas variáveis necessárias
            List<Plano> PopulacaoPais = CopiarPopulacao(PopulacaoClass);          // População de pais
            List<Plano> PopulacaoQt = new List<Plano>();                          // União das populações de pais e filhos
            List<Plano> MelhorSolucaoAt = new List<Plano>();                      // Lista contendo as melhores soluções para cada função objetivo da geração anterior
            List<Plano> MelhorSolucaoAnt = new List<Plano>();                     // Lista contendo as melhores soluções para cada função objetivo da geração atual          
            double somaCusto = 0;                                                 // Soma das custos médios de todas as soluções da geração atual
            double somaEENS = 0;                                                  // Soma dos indíces EENS de todas as soluções da geração atual
            double somaReceita = 0;                                               // Soma das receitas médias de todas as soluções da geração atual
            double medCusto;                                                      // Variável para a média do custo de produção da geração atual
            double medEENS;                                                       // Variável para a média do índice EENS da geração atual
            double medReceita;                                                    // Variável para a média da receita do produtor da geração atual
            double medCustoAnt;                                                   // Variável para a média do custo de produção da geração anterior
            double medEENSAnt;                                                    // Variável para a média do índice EENS da geração anterior
            double medReceitaAnt;                                                 // Variável para a média da receita do produtor da geração anterior
            double limInfCusto;                                                   // Variável para armazenar o limite inferior do custo (99% do custo médio da geração anterior)
            double limSupCusto;                                                   // Variável para armazenar o limite superior do custo (101% do custo médio da geração anterior)
            double limInfEENS;                                                    // Variável para armazenar o limite inferior do índice EENS (99% do índice EENS médio da geração anterior)
            double limSupEENS;                                                    // Variável para armazenar o limite superior do índice EENS (101% do índice EENS da geração anterior)
            double limInfReceita;                                                 // Variável para armazenar o limite inferior da receita (99% da receita média da geração anterior)
            double limSupReceita;                                                 // Variável para armazenar o limite superior da receita (101% da receita média da geração anterior)
            bool estObj1 = false;                                                 // Variável booelana para verificar se há estagnação do primeiro objetivo
            bool estObj2 = false;                                                 // Variável booelana para verificar se há estagnação do segundo objetivo
            bool estObj3 = false;                                                 // Variável booelana para verificar se há estagnação do terceiro objetivo
            int contGer = 1;                                                      // Contador de gerações
            int contRep = 1;                                                      // Contador para verificação da estagnação das melhores soluções
            int contPlano;                                                        // COntador de quantos planos já foram impressos no arquivo contendo todas as gerações da execução atual
            int numMelSol;                                                        // Contador do número de melhores soluções na geração atual

            MelhorSolucaoAnt = MelhoresSolucoes(MelhorSolucaoAt, PopulacaoClass); // Utilizando método para determinar as dez melhores soluções para cada objetivo
            
            numMelSol = MelhorSolucaoAnt.Count() / 4;                             // Determinando a quantidade de indivíduos na lista de melhores soluções
            
            // Loop para acumular os custos da geração inicial
            for(int i = 0; i < numMelSol; i++)
            {
                somaReceita += MelhorSolucaoAnt[i].receitaMediaProdutor;          // Soma a receita média do produtor do i-ésimo indivíduo
            }

            // Loop para acumular os índices EENS da geração inicial
            for(int i = numMelSol; i < 2 * numMelSol; i++)
            {
                somaEENS += MelhorSolucaoAnt[i].eensPlano;                        // Soma o índice EENS do i-ésimo indivíduo
            }

            // Loop para acumular as receitas da geração inicial
            for(int i = 2 * numMelSol; i < 3 * numMelSol; i++)
            {
                somaCusto += MelhorSolucaoAnt[i].custoMedioProd;                  // Soma o custo médio de produção do i-ésimo indivíduo
            }

            // Calculando as médias
            medCustoAnt = somaCusto/10;                                           // Média de custo da geração inicial
            medEENSAnt = somaEENS/10;                                             // Média do índice EENS da geração inicial
            medReceitaAnt = somaReceita/10;                                       // Média de receita da geração inicial

            // Loop de gerações do algortimo NSGA-II
            while(contGer <= gerMax && contRep <= repMax)
            {
                // Unindo a população de pais e filhos
                #region União das populações de pais e filhos

                PopulacaoQt = CopiarPopulacao(PopulacaoPais); // Copia primeiro a população de pais para a população

                // Loop para inserir a população de filhos dentro da população
                for(int i = 0; i < numInd; i++)
                {
                    PopulacaoQt.Add(PopulacaoFilhos[i]);      // Adiciona o i-ésimo filho a população na posição (i + numInd) 
                }

                #endregion

                // Classificando a população entre fronteiras de dominância
                #region Classificação da população em fronteiras de dominância

                // Reiniciando variáveis utilizadas anteriormente
                comprimentoPopulacao = 1;                               // Variável para determinar se ainda há indivíduos para classificar
                ArrayFrontDom = new int[2 * numInd];                    // Array contendo as posições do último indivíduo de cada fronteira
                contPosicoes = 0;                                       // Contador de posições ocupadas da fronteira atual
                PopulacaoDom.Clear();                                   // População auxiliar para os indivíduos dominados da iteração atual
                PopulacaoClass.Clear();                                 // População com os indivíduos já classificados
                
                PopulacaoDom = CopiarPopulacao(PopulacaoQt);            // Copiando a população inicial para a população auxiliar

                // Início das iterações para determinar as fronteiras de dominância
                while (comprimentoPopulacao > 0)
                {
                    FronteiraNaoDom = VerificaDominancia(PopulacaoDom); // Classifica os indivíduos da população auxiliar
                    PopulacaoDomAnt = CopiarPopulacao(PopulacaoDom);    // Armazena a população que já foi classificada
                    PopulacaoDom.Clear();                               // Limpa a população atual para receber a próxima população a ser classificada

                    // Loop para armazenar os indivíduos dominados e não dominados em suas devidas populações
                    for (int i = 0; i < PopulacaoDomAnt.Count; i++)
                    {
                        // Apenas os indivíduos não dominados da iteração atual são adicionados a população classificada
                        if (FronteiraNaoDom[i] == 0)
                        {
                            PopulacaoClass.Add(PopulacaoDomAnt[i]);     // Caso o indivíduo seja não dominado dentro da população atual, é adicionado a população classificada
                        }

                        // Apenas os indivíduos dominados são salvos na população para serem classificados
                        if (FronteiraNaoDom[i] != 0)
                        {
                            PopulacaoDom.Add(PopulacaoDomAnt[i]);       // Caso o indivíduo seja dominado dentro da população atual, é adicionado na população que será classificada na próxima iteração
                        }
                    }

                    comprimentoPopulacao = PopulacaoDom.Count();        // Determinando se ainda há indivíduos que necessitam ser classificados
                    PopulacaoDomAnt.Clear();                            // Limpa a população anterior para a próxima iteração

                    ArrayFrontDom[contPosicoes] = PopulacaoClass.Count; // Armazena no array a posição do último indivíduo da fronteira atual
                    contPosicoes++;                                     // Aumenta o contador de posições ocupadas do array
                }

                // Eliminando posições com valores iguais a zero do array
                nullElem = new int();               // Variável para salvar a posição do array com o último elemento diferente de zero

                // Loop para determinação da posição com o primeiro elemento nulo
                for (int i = (ArrayFrontDom.Length - 1); i >= 0; i--)
                {
                    if (ArrayFrontDom[i] == 0)
                    {
                        nullElem = i;               // Armazena a posição do array com o último elemento diferente de zero
                    }
                }

                FrontDom = new int[nullElem];       // Declara novo array com o número de posições necessárias

                for (int i = 0; i < nullElem; i++)
                {
                    FrontDom[i] = ArrayFrontDom[i]; // Armazena apenas as posições com valores diferente de zero no novo array
                }

                #endregion

                // Calculando Crowding Distance para os indivíduos de uma mesma fronteira
                #region Cálculo de Crowding Distance para os indivíduos da mesma fronteira
                
                // Reiniciando variáveis utilizadas anteriormente
                FronteiraAtual.Clear();                              // População com os indivíduos da fronteira de dominância atual
                crowdingDistance = new double[2*numInd];             // Array para armazenar crowding distance de todos os indivíduos
                contInd = 0;                                         // Contador para a posição do indivíduo dentro de toda a população
                contPos = 0;                                         // Contador de posições preenchidas do array crowdingDistance

                // Loop para varrer o vetor com as fronteiras de dominância e criar as populações de cada uma das fronteiras
                for (int i = 0; i < nullElem; i++)
                {
                    // Loop para adicionar a fronteira os indivíduos pertencentes a ela
                    while (contInd < FrontDom[i])
                    {
                        FronteiraAtual.Add(PopulacaoClass[contInd]); // Adiciona na população da fronteira atual os indivíduos pertencentes a ela
                        contInd++;                                   // Acresce uma unidade ao contador
                    }

                    numEle = FronteiraAtual.Count;                   // Variável para determinar o número de elementos da fronteira atual
                    auxCrowDist = new double[numEle];                // Array auxiliar contendo o mesmo número de elementos que a fronteira atual

                    auxCrowDist = CrowdingDistance(FronteiraAtual);  // Calcula a crowding distance para os indivíduos da fronteira atual

                    // Armazenando os dados do array auxiliar no array principal
                    for (int k = 0; k < numEle; k++)
                    {
                        crowdingDistance[contPos] = auxCrowDist[k];  // Armazena os dados do array auxiliar em seu lugar correspondente
                        contPos++;                                   // Aumenta uma unidade no contador de posições      
                    }

                    FronteiraAtual.Clear();                          // Limpa a população atual para a próxima iteração
                }

                #endregion

                // Selecionando indivíduos para próxima geração de pais
                #region Seleção de indivíduos para compor próxima geração de pais
                
                // Usando o método criado para selecionar os indivíduos da próxima geração de pais
                PopulacaoPais = SelecaoIndividuos(PopulacaoClass, FrontDom, crowdingDistance);
                
                #endregion

                // Classificando a próxima geração entre fronteiras de dominância
                #region Classificação da próxima geração de pais em fronteiras de dominância

                // Reiniciando variáveis utilizadas anteriormente
                comprimentoPopulacao = 1;                               // Variável para determinar se ainda há indivíduos para classificar
                ArrayFrontDom = new int[numInd];                        // Array contendo as posições do último indivíduo de cada fronteira
                contPosicoes = 0;                                       // Contador de posições ocupadas da fronteira atual
                PopulacaoDom.Clear();                                   // População auxiliar para os indivíduos dominados da iteração atual
                PopulacaoClass.Clear();                                 // População com os indivíduos já classificados
                
                PopulacaoDom = CopiarPopulacao(PopulacaoPais);          // Copiando a população inicial para a população auxiliar

                // Início das iterações para determinar as fronteiras de dominância
                while (comprimentoPopulacao > 0)
                {
                    FronteiraNaoDom = VerificaDominancia(PopulacaoDom); // Classifica os indivíduos da população auxiliar
                    PopulacaoDomAnt = CopiarPopulacao(PopulacaoDom);    // Armazena a população que já foi classificada
                    PopulacaoDom.Clear();                               // Limpa a população atual para receber a próxima população a ser classificada

                    // Loop para armazenar os indivíduos dominados e não dominados em suas devidas populações
                    for (int i = 0; i < PopulacaoDomAnt.Count; i++)
                    {
                        // Apenas os indivíduos não dominados da iteração atual são adicionados a população classificada
                        if (FronteiraNaoDom[i] == 0)
                        {
                            PopulacaoClass.Add(PopulacaoDomAnt[i]);     // Caso o indivíduo seja não dominado dentro da população atual, é adicionado a população classificada
                        }

                        // Apenas os indivíduos dominados são salvos na população para serem classificados
                        if (FronteiraNaoDom[i] != 0)
                        {
                            PopulacaoDom.Add(PopulacaoDomAnt[i]);       // Caso o indivíduo seja dominado dentro da população atual, é adicionado na população que será classificada na próxima iteração
                        }
                    }

                    comprimentoPopulacao = PopulacaoDom.Count;          // Determinando se ainda há indivíduos que necessitam ser classificados
                    PopulacaoDomAnt.Clear();                            // Limpa a população anterior para a próxima iteração

                    ArrayFrontDom[contPosicoes] = PopulacaoClass.Count; // Armazena no array a posição do último indivíduo da fronteira atual
                    contPosicoes++;                                     // Aumenta o contador de posições ocupadas do array
                }

                // Eliminando posições com valores iguais a zero do array
                nullElem = new int();               // Variável para salvar a posição do array com o último elemento diferente de zero

                // Loop para determinação da posição com o primeiro elemento nulo
                for (int i = (ArrayFrontDom.Length - 1); i >= 0; i--)
                {
                    if (ArrayFrontDom[i] == 0)
                    {
                        nullElem = i;               // Armazena a posição do array com o último elemento diferente de zero
                    }
                }

                FrontDom = new int[nullElem];       // Declara novo array com o número de posições necessárias

                for (int i = 0; i < nullElem; i++)
                {
                    FrontDom[i] = ArrayFrontDom[i]; // Armazena apenas as posições com valores diferente de zero no novo array
                }

                #endregion

                // Calculando Crowding Distance para os indivíduos de uma mesma fronteira
                #region Cálculo de Crowding Distance para os indivíduos da próxima geração de pais
                
                // Reiniciando variáveis utilizadas anteriormente
                FronteiraAtual.Clear();                              // População com os indivíduos da fronteira de dominância atual
                crowdingDistance = new double[numInd];               // Array para armazenar crowding distance de todos os indivíduos
                contInd = 0;                                         // Contador para a posição do indivíduo dentro de toda a população
                contPos = 0;                                         // Contador de posições preenchidas do array crowdingDistance

                // Loop para varrer o vetor com as fronteiras de dominância e criar as populações de cada uma das fronteiras
                for (int i = 0; i < nullElem; i++)
                {
                    // Loop para adicionar a fronteira os indivíduos pertencentes a ela
                    while (contInd < FrontDom[i])
                    {
                        FronteiraAtual.Add(PopulacaoClass[contInd]); // Adiciona na população da fronteira atual os indivíduos pertencentes a ela
                        contInd++;                                   // Acresce uma unidade ao contador
                    }

                    numEle = FronteiraAtual.Count;                   // Variável para determinar o número de elementos da fronteira atual
                    auxCrowDist = new double[numEle];                // Array auxiliar contendo o mesmo número de elementos que a fronteira atual

                    auxCrowDist = CrowdingDistance(FronteiraAtual);  // Calcula a crowding distance para os indivíduos da fronteira atual

                    // Armazenando os dados do array auxiliar no array principal
                    for (int k = 0; k < numEle; k++)
                    {
                        crowdingDistance[contPos] = auxCrowDist[k];  // Armazena os dados do array auxiliar em seu lugar correspondente
                        contPos++;                                   // Aumenta uma unidade no contador de posições      
                    }

                    FronteiraAtual.Clear();                          // Limpa a população atual para a próxima iteração
                }

                #endregion

                // Gerando indivíduos da população de filhos
                #region Geração de indivíduos da população de filhos

                // Reiniciando variáveis necessárias
                PopulacaoPais.Clear();                                                         // Limpando a população de pais anterior
                PopulacaoFilhos.Clear();                                                       // Limpando a população de filhos anterior
                PopulacaoPais = CopiarPopulacao(PopulacaoClass);                               // Adicionando a população classificada em fronteiras de dominância a população de pais da próxima geração

                pai1 = new Plano(sistema);                                                     // Objeto para receber o primeiro pai
                pai2 = new Plano(sistema);                                                     // Objeto para receber o segundo pai
                contIndPop = new int();
                
                // Loop para cruzar os indivíduos da população
                while(contIndPop < numInd)
                {
                    pai1 = SortearIndividuos(PopulacaoPais, FrontDom, crowdingDistance);       // Usando método para sortear o primeiro pai
                    pai2 = SortearIndividuos(PopulacaoPais, FrontDom, crowdingDistance);       // Usando método para sortear o segundo pai
                    filho1 = new Plano(sistema);                                               // Reiniciando objeto para recebimento do primeiro filho
                    filho2 = new Plano(sistema);                                               // Reiniciando objeto para recebimento do segundo filho

                    genesSort = rd.Next(0, (numGenes - 1));                                    // Sorteia até qual gene as cópias devem ser feitas
                    numSort = rd.NextDouble();                                                 // Sorteia um número para definir se o cruzamento dos pais deve ocorrer
                   
                    // Armazenando os genes dos pais nos filhos
                    for(int j = 0; j < numGenes; j++)
                    {
                        // Armazenando os genes até o gene sorteado
                        if(j <= genesSort)
                        {
                            filho1.vetorPlano[j] = pai1.vetorPlano[j];                         // Primeiro filho recebe os genes do primeiro pai
                            filho2.vetorPlano[j] = pai2.vetorPlano[j];                         // Segundo filho recebe os genes do segundo pai
                        }
                    
                        // Armazenando os genes após o gene sorteado
                        if(j > genesSort)
                        {
                            filho1.vetorPlano[j] = pai2.vetorPlano[j];                         // Primeiro filho recebe os genes do segundo pai
                            filho2.vetorPlano[j] = pai1.vetorPlano[j];                         // Segundo filho recebe os genes do primeiro pai
                        }
                    }
                    
                    filho1 = MutacaoIndividuo(filho1);
                    filho2 = MutacaoIndividuo(filho2);

                    filho1.AvaliaPlano(SMCNS);                                                 // Avalia o plano gerado através do método
                    filho2.AvaliaPlano(SMCNS);                                                 // Avalia o plano gerado através do método
                    
                    planoExistente1 = VerificaPlanoExisteLista(PopulacaoPais, filho1);         // Usando método para ver se o indivíduo existe na população de pais
                    planoExistente2 = VerificaPlanoExisteLista(PopulacaoPais, filho2);         // Usando método para ver se o indivíduo existe na população de pais
                    planoExistente3 = VerificaPlanoExisteLista(PopulacaoFilhos, filho1);       // Usando método para ver se o indivíduo existe na população de filhos
                    planoExistente4 = VerificaPlanoExisteLista(PopulacaoFilhos, filho2);       // Usando método para ver se o indivíduo existe na população de filhos

                    semanasIguais1 = VerificaCronograma(filho1, arrayUsinas, arrayNSemNecMnt); // Usando método para ver se o primeiro indivíduo gerado não possui geradores da mesma usina programados para manutenção em um mesmo intervalo
                    semanasIguais2 = VerificaCronograma(filho2, arrayUsinas, arrayNSemNecMnt); // Usando método para ver se o segundo indivíduo gerado não possui geradores da mesma usina programados para manutenção em um mesmo intervalo
                    
                    // Adicionando apenas os filhos que não existam nas populações de pais e filhos, caso o número sorteado seja menor que a probabilidade
                    if(numSort <= crossRate && planoExistente1 == false && planoExistente2 == false && planoExistente3 == false && planoExistente4 == false && semanasIguais1 == false && semanasIguais2 == false)
                    {
                        PopulacaoFilhos.Add(filho1);                                           // Adiciona o indivíduo gerado a população de filhos
                        PopulacaoFilhos.Add(filho2);                                           // Adiciona o indivíduo gerado a população de filhos
                        contIndPop = PopulacaoFilhos.Count();
                    }

                    pai1 = new Plano(sistema);                                                 // Reiniciando plano para recebimento do primeiro pai
                    pai2 = new Plano(sistema);                                                 // Reiniciando plano para recebimento do segundo pai
                }

                #endregion
                
                // Verificando se há estagnação dos resultados na geração atual
                #region Verificação da possibilidade de haver estagnação das soluções

                // Reiniciando variáveis necessárias
                MelhorSolucaoAt.Clear();                                             // Reiniciando lista para recebimento das melhores soluções
                somaCusto = 0;                                                       // Reiniciando variável para soma dos custos da geração atual
                somaEENS = 0;                                                        // Reiniciando variável para soma dos índices EENS da geração atual
                somaReceita = 0;                                                     // Reiniciando variável para soma das receitas da geração atual
                estObj1 = false;                                                     // Reiniciando variável booleana para o primeiro objetivo
                estObj2 = false;                                                     // Reiniciando variável booleana para o segundo objetivo
                estObj3 = false;                                                     // Reiniciando variável booleana para o terceiro objetivo
                
                MelhorSolucaoAt = MelhoresSolucoes(MelhorSolucaoAnt, PopulacaoPais); // Utilizando método para determinar as dez melhores soluções para cada objetivo
                MelhorSolucaoAnt.Clear();                                            // Limpando lista para atualização
                MelhorSolucaoAnt = CopiarPopulacao(MelhorSolucaoAt);                 // Atualizando lista com melhores soluções anteriores para próxima geração

                numMelSol = MelhorSolucaoAnt.Count() / 4;                            // Determinando a quantidade de indivíduos na lista de melhores soluções

                for(int i = 0; i < numMelSol; i++)
                {
                    somaReceita += MelhorSolucaoAt[i].receitaMediaProdutor;          // Soma a receita média do produtor do i-ésimo indivíduo
                }

                // Loop para acumular os índices EENS da geração inicial
                for(int i = numMelSol; i < 2 * numMelSol; i++)
                {
                    somaEENS += MelhorSolucaoAt[i].eensPlano;                        // Soma o índice EENS do i-ésimo indivíduo
                }

                // Loop para acumular as receitas da geração inicial
                for(int i = 2 * numMelSol; i < 3 * numMelSol; i++)
                {
                    somaCusto += MelhorSolucaoAt[i].custoMedioProd;                  // Soma o custo médio de produção do i-ésimo indivíduo
                }

                medReceita = somaReceita/10;                                         // Calculando a média para os valores da primeira função objetivo
                medEENS = somaEENS/10;                                               // Calculando a média para os valores da segunda função objetivo
                medCusto = somaCusto/10;                                             // Calculando a média para os valores da terceira função objetivo

                
                fileMedRec.WriteLine("{0:0000.0000}", medReceita);
                fileMedEENS.WriteLine("{0:000000.0000}", medEENS);
                fileMedCus.WriteLine("{0:000.0000}", medCusto);

                
                // Calculando limites das funções objetivos para comparação
                limInfCusto = (1 - tol) * medCustoAnt;                               // Limite inferior do custo
                limSupCusto = (1 + tol) * medCustoAnt;                               // Limite superior do custo
                limInfEENS = (1 - tol) * medEENSAnt;                                 // Limite inferior do índice EENS
                limSupEENS = (1 + tol) * medEENSAnt;                                 // Limite superior do índice EENS
                limInfReceita = (1 - tol) * medReceitaAnt;                           // Limite inferior da receita
                limSupReceita = (1 + tol) * medReceitaAnt;                           // Limite superior da receita

                // Caso a média do custo atual esteja no intervalor de 99,9% da média de custo anterior e 100,1% da média de custo anterior
                if(medCusto >= limInfCusto && medCusto <= limSupCusto)
                {
                    estObj1 = true;                                                  // Objetivo 1 estagnado
                }

                // Caso a média do custo atual esteja no intervalor de 99,9% da média de custo anterior e 100,1% da média de custo anterior
                if(medEENS >= limInfEENS && medEENS <= limSupEENS)
                {
                    estObj2 = true;                                                  // Objetivo 2 estagnado
                }

                // Caso a média do custo atual esteja no intervalor de 99,9% da média de custo anterior e 100,1% da média de custo anterior
                if(medReceita >= limInfReceita && medReceita <= limSupReceita)
                {
                    estObj3 = true;                                                  // Objetivo 3 estagnado
                }

                // Caso todos os objetivos estejam estagnados
                if(estObj1 == true && estObj2 == true && estObj3 == true)
                {
                    contRep++;                                                       // Aumenta-se o contador de repetições de soluções estagnadas
                }

                // Condição para reiniciar contador
                if(estObj1 == false || estObj2 == false || estObj3 == false || MelhorSolucaoAt.Count() != 40)
                {
                    contRep = 1;                                                     // Contador reiniciado
                }

                medCustoAnt = medCusto;                                              // Atualiza a variável para próxima geração
                medEENSAnt = medEENS;                                                // Atualiza a variável para próxima geração
                medReceitaAnt = medReceita;                                          // Atualiza a variável para próxima geração

                #endregion

                // Imprimindo i-ésima geração da execução atual no arquivo
                #region Impressão dos resultados no arquivo de saída
                
                contPlano = 0;                                                // Reiniciando contador de planos de manutenção da geração atual
                
                fileIndExec.WriteLine("{000}ª GERAÇÃO:", contGer);            // Escrevendo linha com informação do número da geração atual
                fileIndExec.Write("   ");

                // Loop para imprimir alguns títulos das colunas relativas a cada unidade geradora que deve receber manuteção
                foreach (Gerador gerador in this.sistema.Geradores)
                {
                    // Para cada gerador que deve ser programado para receber manutenção
                    if (gerador.progMnt == true) 
                    {
                        fileIndExec.Write("    {0}", "UG");                   // Imprime UG (Unidade Geradora) na i-ésima coluna
                    }
                }

                fileIndExec.WriteLine();                                      // Mudando de linha no arquivo de saída
                fileIndExec.Write(" CM");                                     // CM: Cronograma de manutenção

                // Loop para imprimir restante dos títulos das colunas relativas a cada unidade geradora que deve receber manuteção
                foreach (Gerador gerador in this.sistema.Geradores)
                {
                    // Para cada gerador que deve ser programado para receber manutenção
                    if (gerador.progMnt == true)
                    {
                        fileIndExec.Write("   {0:000}", gerador.PotMax);      // Imprime a potência máxima da i-ésima unidade a receber manutenção
                    }
                }

                // Imprimindo título das colunas restantes
                fileIndExec.WriteLine("    {0}       {1}       {2}", "Custo Médio", "EENS", "Receita Média");

                // Imprimindo informações sobre o j-ésimo indivíduo da geração atual
                foreach (Plano plano in MelhorSolucaoAt)
                {
                    contPlano++;                                              // Aumentando o contador de planos já impressos
                    fileIndExec.Write("{0:000}", contPlano);                  // Escrevendo o número do plano atual

                    // Escrevendo informações do vetor plano de manutenção
                    for (int i = 0; i < plano.vetorPlano.Length; i++)
                    {
                        fileIndExec.Write("    {0:00}", plano.vetorPlano[i]); // Escreve a semana em que o gerador atual deve receber manutenção 
                    }
                    
                    // Escrevendo os valores das funções objetivo do j-ésimo indivíduo
                    fileIndExec.WriteLine("     {0:000.00000}    {1:000000.00000}    {2:0000.00000}", plano.custoMedioProd, plano.eensPlano, plano.receitaMediaProdutor);
                }

                // Escrevendo as médias dos valores dos objetivos das melhores soluções
                fileIndExec.WriteLine("####");
                fileIndExec.WriteLine("Média das melhores soluções para a receita do produtor: {0:0000.0000}", medReceita);
                fileIndExec.WriteLine("Média das melhores soluções para o índice EENS: {0:000000.0000}", medEENS);
                fileIndExec.WriteLine("Média das melhores soluções para o custo de produção: {0:000.0000}", medCusto);

                // Escrevendo número de repetições das melhores soluções, caso haja repetição
                fileIndExec.WriteLine("Número de repetições das melhores soluções: {00}", contRep);
                
                // Saltando linhas para impressão da próxima geração
                fileIndExec.WriteLine();
                fileIndExec.WriteLine();

                #endregion

                contGer++;       // Acrescendo uma unidade ao contador de gerações
            }

            fileIndExec.Close(); // Fechando arquivo criado para escrever as informações de cada uma das gerações
            fileMedRec.Close();
            fileMedEENS.Close();
            fileMedCus.Close();

            #endregion

            // Retornando os resultados obtidos pelo método
            #region Organização e retorno das soluções

            // Organizando soluções dos geradores de mesma usina
            foreach(Plano planoPop in MelhorSolucaoAt)
            {
                planoPop.OrdenaPlanoAvalConf(); // Utiliza o método criado para ordenar as soluções de uma mesma usina
            }

            // Escrevendo o número de gerações no console
            Console.WriteLine(" Número de gerações necessárias: {0}", contGer - 1);

            // Escrevendo número de repetições no console
            Console.WriteLine(" Número de repetições das melhores soluções: {0}", contRep - 1);
            
            // Armazenando o número de repetições realizadas no objeto da classe dos dados do sistema
            this.sistema.testeAtual.repeticoes = contRep - 1;

            // Armazenando o número de gerações necessárias no objeto da classe da rotina de otimização
            this.sistema.testeAtual.geracoes = contGer - 1;

            // Adicionando indivíduo do caso base ao final da população da execução atual
            MelhorSolucaoAt.Add(indCasoBase);

            // Retornar população com as melhores soluções
            return MelhorSolucaoAt;

            #endregion
        }
        #endregion
        // ============================================================================================

        // ------------------------------------------------
        // Métodos auxiliares das ferramentas de otimização
        #region Métodos gerais de auxílio às ferramentas de otimização

        // ------------------------------------------------
        // Ordena planos em uma lista pela aptidão (devolve lista de planos ordenados pela aptidão)
        public List<Plano> OrdenaListaPlanosAptidao(List<Plano> _ListaOrdenar)
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
        public bool VerificaPlanosIguais(Plano _plano1, Plano _plano2)
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
        public bool VerificaPlanoExisteLista(List<Plano> _listaPlano, Plano _planoComp)
        {
            bool existe = false;
            _planoComp.OrdenaPlanoAvalConf();      // Garantindo que semanas de início da manutenção tenham sido ordenadas antes da verificação
            foreach (Plano plano in _listaPlano)
            {
                if (VerificaPlanosIguais(plano, _planoComp) == true)
                { // Plano é igual a algum plano da lista
                    existe = true;
                    break;
                }
            }
            return existe;
        }

        // ------------------------------------------------
        // Cria cópia de plano (devolve um plano idêntico, porém ainda não avaliado
        public Plano CopiaPlano(Plano _plano)
        {
            Plano planoCopia = new Plano(sistema);
            for (int i = 0; i < _plano.vetorPlano.Length; i++)
            {
                planoCopia.vetorPlano[i] = _plano.vetorPlano[i];
            }
            return planoCopia;
        }

        // ------------------------------------------------
        // Cria roleta para emprego do operador Cruzamento
        public double[] CriaRoleta(List<Plano> _PopAtual)
        {
            double[] roleta = new double[_PopAtual.Count()];

            double somAptidao = 0;
            for (int i = 0; i < _PopAtual.Count(); i++)
            {
                roleta[i] = _PopAtual[i].aptidao;
                somAptidao += _PopAtual[i].aptidao;
            }
            roleta[0] = (somAptidao - roleta[0]) / ((_PopAtual.Count() - 1) * somAptidao);
            for (int i = 1; i < _PopAtual.Count(); i++)
            {
                roleta[i] = roleta[i - 1] + (somAptidao - roleta[i]) / ((_PopAtual.Count() - 1) * somAptidao);
            }
            return roleta;
        }
        #endregion

        // ------------------------------------------------
        // Métodos específicos da técnica AG
        #region Métodos específicos de auxílio à técnica AG

        // Operador de Cruzamento Uniforme para o AG
        public List<Plano> CruzamentoUniforme(Plano _planoPai1, Plano _planoPai2)
        {
            List<Plano> PlanosFilhos = new List<Plano>();
            Plano planoFilho1 = new Plano(sistema);         // Filho 1
            Plano planoFilho2 = new Plano(sistema);         // Filho 2

            double y = rd.NextDouble();                     // Variável aleatória para decisão de cruzamento
            if (y <= taxaCruz)
            {
                // Realiza cruzamento uniforme (máscara de bits)
                for (int i = 0; i < _planoPai1.vetorPlano.Count(); i++)
                {
                    double x = rd.NextDouble();
                    if (x < 0.5)
                    {
                        planoFilho1.vetorPlano[i] = _planoPai1.vetorPlano[i];
                        planoFilho2.vetorPlano[i] = _planoPai2.vetorPlano[i];
                    }
                    else
                    {
                        planoFilho1.vetorPlano[i] = _planoPai2.vetorPlano[i];
                        planoFilho2.vetorPlano[i] = _planoPai1.vetorPlano[i];
                    }
                }
            }
            else
            {
                // Não realiza cruzamento. Filhos são cópias dos pais
                planoFilho1 = CopiaPlano(_planoPai1);
                planoFilho2 = CopiaPlano(_planoPai2);
            }
            PlanosFilhos.Add(planoFilho1);
            PlanosFilhos.Add(planoFilho2);

            return PlanosFilhos;
        }

        // Operador de Mutação para o AG
        public Plano Mutacao(Plano _plano)
        {
            Plano planoMutado = new Plano(sistema);

            //for (int i = 0; i < planoMutado.vetorPlano.Count(); i++)
            foreach (Gerador ger in sistema.GeradoresMnt)
            {
                double x = rd.NextDouble();                         // Variável aleatória para decisão de mutação
                if (x < taxaMut)
                { // Gene será mutado
                    if (_plano.vetorPlano[ger.posvetorPlano] == ((sistema.semFimMnt - sistema.semIniMnt + 1) - ger.nSemNecMnt + 1))
                    { // Se a manutenção do gerador encontra-se programada para a última semana possível, atrase uma semana
                        planoMutado.vetorPlano[ger.posvetorPlano] = _plano.vetorPlano[ger.posvetorPlano] - 1;
                    }
                    else if (_plano.vetorPlano[ger.posvetorPlano] == 1)
                    { // Se a manutenção do gerador encontra-se programada para a primeira semana do período de estudo, adiante uma semana
                        planoMutado.vetorPlano[ger.posvetorPlano] = _plano.vetorPlano[ger.posvetorPlano] + 1;
                    }
                    else
                    { // Caso contrário, faça novo sorteio para decidir, com igual probabilidade, se a manunteção será adiantada ou atrasada em uma semana
                        double y = rd.NextDouble();
                        if (y < 0.5)
                        {
                            planoMutado.vetorPlano[ger.posvetorPlano] = _plano.vetorPlano[ger.posvetorPlano] - 1;
                        }
                        else
                        {
                            planoMutado.vetorPlano[ger.posvetorPlano] = _plano.vetorPlano[ger.posvetorPlano] + 1;
                        }
                    }
                }
                else
                { // gene não será mutado
                    planoMutado.vetorPlano[ger.posvetorPlano] = _plano.vetorPlano[ger.posvetorPlano];
                }
            }

            return planoMutado;
        }

        #endregion

        // ------------------------------------------------
        // Métodos de auxílio específicos da técnica NSGA-II
        #region Métodos específicos de auxílio à técnica NSGA-II

        // Otimização via AG de cada um dos objetivos para comparação das soluções da NSGA-II
        #region Método de otimização via AG para um dos três objetivos
        public Plano OtimizacaoAG(int _objetivo)
        {
            
            // Instanciando parâmetros necessários para o AG
            Plano melhorSolucao = new Plano(sistema);              // Plano de manutenção a ser retornado contendo o melhor indivíduo segundo um dos objetivos
            double melhorObjetivoAnt = 0;                          // Variável para armazenar o valor do objetivo da melhor solução da geração anterior
            double melhorObjetivoAt = 0;                           // Variável para armazenar o valor do objetivo da melhor solução da geração atual

            int numInd = 50;                                       // Número de indivíduos da população
            int gerMax = 100;                                      // Número máximo de gerações
            int repMax = 100;                                      // Número máximo de repetições
            this.taxaCruz = 0.7;                                   // Taxa de cruzamento para a AG
            this.taxaMut = 0.15;                                   // Taxa de mutação para a AG
            int numEleMut = this.sistema.testeAtual.numEleMut;     // Número de elementos do array a serem mutados
            int semIni = this.sistema.testeAtual.semIniMnt;        // Semana inicial do período de estudo
            int semFim = this.sistema.testeAtual.semFimMnt;        // Semana final de período de estudo
            string perEst = this.sistema.testeAtual.periodoEst;    // Variável para o período de estudo
            
            // Geração de indivíduos com maior chance de atenderem aos valores das funções objetivo
            #region Gerando indivíduos com maior chance de terem bons valores para cada função objetivo

            // Instanciando algumas variáveis necessárias
            List<Gerador> UGsMnt = this.sistema.GeradoresMnt;                          // Lista com as unidades geradoras do sistema
            List<Plano> PopIntel = new List<Plano>();                                  // Lista contendo os indivíduos com maiores chances de ter bons valores das funções objetivo
            Plano indIntel = new Plano(sistema);                                       // Objeto para receber suposto melhor plano para receita média do produtor
            double[] PLDSemanal = new double[this.sistema.PLDMedioSemanal.Length - 1]; // Array para o PLD médio semanal
            double[] curvaDeCarga = this.sistema.curvaCarga;                           // Array para a curva de carga
            double[] curvaCargaSemanal = new double[PLDSemanal.Length];                // Array para a curva de carga média semanal
            double[] potMaxGer = new double[UGsMnt.Count()];                           // Array para as potências máximas de cada unidade geradora a receber manutenção
            double[] custoGer = new double[UGsMnt.Count()];                            // Array para os custos de produção de cada unidade geradora a receber manutenção
            int[] posicoesGerPot = new int[UGsMnt.Count()];                            // Array para as posições de cada unidade geradora a receber manutenção ordenado pela potência máxima
            int[] posicoesGerCusto = new int[UGsMnt.Count()];                          // Array para as posições de cada unidade geradora a receber manutenção ordenado pelo custo de geração
            int[] posicoesPLD = new int[PLDSemanal.Length];                            // Array para conter as semanas ordenadas pelos valores do PLD
            int[] posicoesCurva = new int[PLDSemanal.Length];                          // Array para conter as semanas ordenadas pelos valores da curva de carga média semanal
            int contHoras = new int();                                                 // Contador de horas
            double somPot = new double();                                              // Variável para se acumular as cargas de toda uma semana
            
            // Armazenando valores do PLD semanal no array correspondente
            for(int i = 0; i < PLDSemanal.Length; i++)
            {
                PLDSemanal[i] = this.sistema.PLDMedioSemanal[i];                       // Armazena o valor do PLD da i-ésima semana 
            }

            // Criando curva de carga semanal
            for (int i = 0; i < (PLDSemanal.Length); i++)
            {
                somPot = 0;                                                            // Reiniciando variável para acumular as pôtências de toda uma semana

                for(int j = contHoras; j < (contHoras + 24 * 7); j++)
                {
                    somPot += curvaDeCarga[j];                                         // Armazena o valor da potência da j-ésima hora do período
                }

                curvaCargaSemanal[i] = somPot/(24 * 7);                                // Calcula o valor médio de carga para a i-ésima semana
                contHoras += 24*7;                                                     // Acresce o número de horas dentro de uma semana no contador

                // Condição para interromper o loop for
                if(contHoras > ((PLDSemanal.Length) * 24 * 7))         
                {
                    break;
                }
            }

            // Ordenando array de posições do PLD de acordo com as semanas com menores valores
            for(int i = 0; i < PLDSemanal.Length; i++)
            {
                posicoesPLD[i] = i;                                                    // Adiciona i-ésima semana ao array
            }

            Array.Sort(PLDSemanal, posicoesPLD);

            // Ordenando array de posições da curva de carga de acordo com as semanas com menores valores
            for(int i = 0; i < PLDSemanal.Length - 1; i++)
            {
                posicoesCurva[i] = i;                                                  // Adiciona i-ésima semana ao array
            }

            Array.Sort(curvaCargaSemanal, posicoesCurva);

            // Compondo arrays para ordenar geradores em ordem decrescente de potência máxima
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                posicoesGerPot[i] = i;                                                 // Adiciona posição do i-ésimo gerador ao array
                potMaxGer[i] = UGsMnt[i].PotMax;                                       // Adiciona potência do i-ésimo gerador ao array
            }

            Array.Sort(potMaxGer, posicoesGerPot);                                     // Ordenando arrays em ordem crescente do custo de produção
            Array.Reverse(posicoesGerPot);                                             // Invertendo a ordem do array de posições

            
            // Compondo arrays para ordenar geradores em ordem decrescente de custo
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                posicoesGerCusto[i] = i;
                custoGer[i] = UGsMnt[i].custoGer;
            }

            Array.Sort(custoGer, posicoesGerCusto);                                    // Ordenando arrays em ordem crescente do custo de produção
            Array.Reverse(posicoesGerCusto);                                           // Invertendo a ordem do array de posições

            // Loop para gerar indivíduo com possível maior receita
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                indIntel.vetorPlano[posicoesGerPot[i]] = posicoesPLD[i] + 1;           // Geradores com maior potência colocados para manutenção nas semanas com menor valor do PLD
            }

            indIntel.AvaliaPlano(SMCNS);                                               // Avaliando plano criado através do método
            PopIntel.Add(indIntel);                                                    // Adicionando plano à população inteligente
            indIntel = new Plano(sistema);                                             // Reiniciando objeto para recebimento do próximo plano

            // Gerando indivíduo com possível menor índice EENS
            for(int i = 0; i < UGsMnt.Count(); i ++)
            {
                indIntel.vetorPlano[posicoesGerPot[i]] = posicoesCurva[i] + 1;         // Geradores com maior potência colocados para manutenção nas semanas com menor carga
            }

            indIntel.AvaliaPlano(SMCNS);                                               // Avaliando plano criado através do método
            PopIntel.Add(indIntel);                                                    // Adicionando plano à população inteligente
            indIntel = new Plano(sistema);                                             // Reiniciando objeto para recebimento do próximo plano

            // Gerando indivíduo com possível menor custo médio de produção
            for(int i = 0; i < UGsMnt.Count(); i ++)
            {
                indIntel.vetorPlano[posicoesGerCusto[i]] = posicoesCurva[i] + 1;       // Geradores com maior potência colocados para manutenção nas semanas com menor carga
            }
            
            indIntel.AvaliaPlano(SMCNS);                                               // Avaliando plano criado através do método
            PopIntel.Add(indIntel);                                                    // Adicionando plano à população inteligente
            indIntel = new Plano(sistema);                                             // Reiniciando objeto para recebimento do próximo plano

            // Gerando indivíduos para as semanas com menor valor do PLD (todas posições do vetor serão colocadas na mesma semana)
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                for(int j = 0; j < UGsMnt.Count(); j++)
                {
                    indIntel.vetorPlano[j] = posicoesPLD[i] + 1;
                }

                indIntel.AvaliaPlano(SMCNS);                                           // Avaliando plano criado através do método
                PopIntel.Add(indIntel);                                                // Adicionando plano à população inteligente
                indIntel = new Plano(sistema);                                         // Reiniciando objeto para recebimento do próximo plano
            }

            // Gerando indivíduos para as semanas com menor carga média (todas posições do vetor serão colocadas na mesma semana)
            for(int i = 0; i < UGsMnt.Count(); i++)
            {
                for(int j = 0; j < UGsMnt.Count(); j++)
                {
                    indIntel.vetorPlano[j] = posicoesCurva[i] + 1;
                }

                indIntel.AvaliaPlano(SMCNS);                                           // Avaliando plano criado através do método                                       
                PopIntel.Add(indIntel);                                                // Adicionando plano à população inteligente
                indIntel = new Plano(sistema);                                         // Reiniciando objeto para recebimento do próximo plano
            }

            #endregion

            //Compondo geração inicial
            #region Composição da população inicial

            // Instanciando variáveis necessárias
            List<Plano> PopInicial = new List<Plano>();                                    // Lista com os planos de manutenção (indivíduos) da geração inicial
            PopInicial.AddRange(PopIntel);                                                 // Adicionando população inteligente à população inicial
            bool planoExistente;                                                           // Variável booleana para determinar se um plano existente foi gerado
            int numSemPeriodo = 17 + 4;                                                    // Número de semanas do período de estudo úmido (17 semanas no ínicio mais 4 semanas no fim);
            double probIni = (Convert.ToDouble(semIni) / Convert.ToDouble(numSemPeriodo)); // Probabilidade de o número sorteado estar no início do período
            double xProb;                                                                  // Variável para receber um sorteio da semana
            int semMax;                                                                    // Variável com a semana máxima em que a manutenção pode ocorrer para o atual gerador

            // Compondo geração inicial para o período úmido
            if(perEst == "UMID")
            {
                for (int i = PopIntel.Count(); i < numInd; i++)
                {
                    planoExistente = true;                                                 // Reiniciando variável booleana

                    // Loop será mantido enquanto um plano diferente dos existentes na lista não seja criado não seja criado
                    while(planoExistente == true)
                    {
                        Plano plano = new Plano(sistema);

                        // Loop para sorteio de semanas para os genes do indivíduo atual
                        for (int j = 0; j < plano.vetorPlano.Length; j++)
                        {
                            xProb = rd.NextDouble();                                       // Sorteado um número para definir se o j-ésimo gene estará no ínicio ou fim do período de estudo

                            // Caso o número sorteado seja menor que a possibilidade de estar no início
                            if(xProb <= probIni)
                            {
                                semMax = semIni - UGsMnt[j].nSemNecMnt;
                                int semana = rd.Next(1, semMax);                           // Sorteado uma semana aleatória entre as semanas inicial e final
                                plano.vetorPlano[j] = semana;                              // O número da semana sorteado é armazenado na posição atual do array
                            }

                            // Caso o número sorteado seja maior que a probabilidade de estar no início
                            else if(xProb > probIni)
                            {
                                semMax = 52 - UGsMnt[j].nSemNecMnt;
                                int semana = rd.Next(semFim, semMax);                      // Sorteado uma semana aleatória entre as semanas inicial e final
                                plano.vetorPlano[j] = semana;                              // O número da semana sorteado é armazenado na posição atual do array
                            }
                        }

                        plano.AvaliaPlano(SMCNS);                                          // Avalia o plano, e calcula o seu valor para as três funções objetivo
                        planoExistente = VerificaPlanoExisteLista(PopInicial, plano);      // Usando método para verificar se o plano existe na população inicial

                        // Condição para adicionar o plano criado a população inicial
                        if(planoExistente == false)
                        {
                            PopInicial.Add(plano);                                         // Adiciona o plano criado à população inicial
                        }
                    }
                }
            }

            // Compondo população inicial para os demais períodos
            else
            {
                // Loop para geração dos indivíduos da primeira geração
                for (int i = PopIntel.Count(); i < numInd; i++)
                {
                    planoExistente = true;                                                 // Reiniciando variável booleana

                    // Loop será mantido enquanto um plano diferente dos existentes na lista não seja criado não seja criado
                    while(planoExistente == true)
                    {
                        Plano plano = new Plano(sistema);

                        // Loop para sorteio de semanas para os genes do indivíduo atual
                        for (int j = 0; j < plano.vetorPlano.Length; j++)
                        {
                            semMax = semFim - UGsMnt[j].nSemNecMnt;
                            int semana = rd.Next(semIni, semMax);                          // Sorteado uma semana aleatória entre as semanas inicial e final
                            plano.vetorPlano[j] = semana;                                  // O número da semana sorteado é armazenado na posição atual do array
                        }

                        plano.AvaliaPlano(SMCNS);                                          // Avalia o plano, e calcula o seu valor para as três funções objetivo
                        planoExistente = VerificaPlanoExisteLista(PopInicial, plano);      // Usando método para verificar se o plano existe na população inicial

                        // Condição para adicionar o plano criado a população inicial
                        if(planoExistente == false)
                        {
                            PopInicial.Add(plano);                                         // Adiciona o plano criado à população inicial
                        }
                    }
                }
            }

            // Organizando a população inicial segundo o objetivo escolhido
            PopInicial = OrganizaIndividuos(PopInicial, _objetivo);                        // Usando método para organizar a população de acordo com os valores da função objetivo escolhida
            
            // Determinando melhor solução e melhor valor da função objetivo da população inicial
            melhorSolucao = PopInicial[0];                                                 // Armazena o primeiro indivíduo como sendo o melhor plano para o objetivo atual

            // Caso o objetivo sendo otimizado seja a receita média do produtor
            if(_objetivo == 1)
            {
                melhorObjetivoAnt = -melhorSolucao.receitaMediaProdutor;                      // Armazenando seu valor
            }

            // Caso o objetivo sendo otimizado seja o custo médio de produção
            else if(_objetivo == 2)
            {
                melhorObjetivoAnt = melhorSolucao.eensPlano;                                  // Armazenando seu valor
            }

            // Caso o objetivo sendo otimizado seja o índice EENS
            else if(_objetivo == 3)
            {
                melhorObjetivoAnt = melhorSolucao.custoMedioProd;                             // Armazenando seu valor
            }

            #endregion

            // Iniciando as iterações do AG
            #region Início das iterações da AG

            // Instanciando algumas variáveis necessárias
            List<Plano> GeracaoAtual = new List<Plano>(); // População de pais da geração atual
            List<Plano> GeracaoNova = new List<Plano>();  // População de filhos da geração atual
            Plano pai1;
            Plano pai2;
            Plano filho1;
            Plano filho2;

            bool x1Encontrado;                            // Variável booleana para determinar se primeiro pai foi determinado
            bool x2Encontrado;                            // Variável booleana para determinar se segundo pai foi determinado
            int contGer = 1;                              // Contador de gerações
            int contRep = 1;                              // Contador de repetições da melhor solução
            double x1;                                    // Variável de sorteio para o primeiro pai
            double x2;                                    // Variável de sorteio para o segundo pai
            double[] roletaSel;                           // Array contendo a roleta de seleção dos indivíduos

            GeracaoAtual = CopiarPopulacao(PopInicial);   // Copiando população inicial para a população da primeira geração

            // Loop iterativo do algoritmo genético
            while(contGer <= gerMax && contRep <= repMax)
            {
                // Cruzamento e mutação para gerar novos indivíduos
                #region Cruzamento e mutação dos indivíduos

                GeracaoNova.Clear();                      // Limpando a nova geração para recebimento de novos indivíduos
                roletaSel = CriarRoletaSelecao(numInd);   // Criando array com as probabilidades de seleção de cada um dos indivíduos
                
                // Loop para composição da nova geração
                while(GeracaoNova.Count() < numInd)
                {
                    pai1 = new Plano(sistema);            // Reiniciando objeto para recebimento do primeiro pai
                    pai2 = new Plano(sistema);            // Reiniciando objeto para recebimento do segundo pai
                    filho1 = new Plano(sistema);          // Reiniciando objeto para recebimento do primeiro filho
                    filho2 = new Plano(sistema);          // Reiniciando objeto para recebimento do segundo filho
                    x1Encontrado = false;                 // Reiniciando primeira variável booleana 
                    x2Encontrado = false;                 // Reiniciando primeira variável booleana
                    x1 = rd.NextDouble();                 // Sorteando número para determinar o primeiro pai
                    x2 = rd.NextDouble();                 // Sorteando número para determinar o segundo pai
                    
                    // Loop para determinar quais indivíduos foram sorteados para serem pais
                    for (int i = 1; i < roletaSel.Count(); i++)
                    {
                        // Selecionando primeiro pai
                        if (roletaSel[i] > x1 && x1Encontrado == false)
                        {
                            pai1 = GeracaoAtual[i];       // Armazenando primeiro indivíduo escolhido para ser pai
                            x1Encontrado = true;          // Mudando estado da variável booleana, uma vez que o primeiro pai foi selecionado
                        }

                        // Selecionando primeiro pai
                        if (roletaSel[i] > x2 && x2Encontrado == false)
                        {
                            pai2 = GeracaoAtual[i];       // Armazenando primeiro indivíduo escolhido para ser pai
                            x2Encontrado = true;          // Mudando estado da variável booleana, uma vez que o segundo pai foi selecionado
                        }

                        // Condição para terminar o loop caso ambos os pais tenham sido selecionados
                        if (x1Encontrado == true && x2Encontrado == true) 
                        {
                            break;
                        }
                    }

                    // Verificando se os indivíduos selecionados são iguais
                    if (VerificaPlanosIguais(pai1, pai2) == true)
                    {
                        continue;                         // Caso os indivíduos sejam iguais, repete-se o loop while para encontrar novos indivíduos
                    }

                    // Realizando o cruzamento dos pais selecionados
                    List<Plano> PlanosFilhos = CruzamentoUniforme(pai1, pai2);
                    filho1 = PlanosFilhos[0];
                    filho2 = PlanosFilhos[1];

                    // Realizando a mutação dos filhos gerados pelo cruzamento
                    filho1 = Mutacao(filho1);
                    filho2 = Mutacao(filho2);

                    // Caso o primeiro indivíduo gerado não exista em nenhuma das duas populações, pode ser adicionado a população
                    if (VerificaPlanoExisteLista(GeracaoAtual, filho1) == false && VerificaPlanoExisteLista(GeracaoNova, filho1) == false)
                    {
                        filho1.AvaliaPlano(SMCNS);
                        GeracaoNova.Add(filho1);
                    }

                    // Caso o segundo indivíduo gerado não exista em nenhuma das duas populações, pode ser adicionado a população
                    if (VerificaPlanoExisteLista(GeracaoAtual, filho2) == false && VerificaPlanoExisteLista(GeracaoNova, filho2) == false)
                    { 
                        filho2.AvaliaPlano(SMCNS);
                        GeracaoNova.Add(filho2);
                    }
                }

                #endregion

                // Selecionando indivíduos para próxima geração
                #region Seleção dos indivíduos para a próxima geração

                GeracaoNova.AddRange(GeracaoAtual);                       // Adicionando indivíduos da população atual a nova geração para classificação
                GeracaoNova = OrganizaIndividuos(GeracaoNova, _objetivo); // Classificando os indivíduos de acordo com os valores da função objetivo selecionada
                GeracaoAtual.Clear();

                for(int i = 0; i < numInd; i++)
                {
                    GeracaoAtual.Add(GeracaoNova[i]);
                }

                #endregion

                contGer++; // Aumentando o contador de gerações
            }

            melhorSolucao = new Plano(sistema);
            melhorSolucao = GeracaoNova[0];

            #endregion
            
            return melhorSolucao;
        }

        //----------------------------------------------------
        // Métodos para auxiliar a ferramenta de otimização AG
        #region Métodos de auxílio a ferramenta de otimização AG
        
        // Método para organizar população segundo um dos objetivos
        #region Método para organizar uma população segundo um dos objetivos

        public List<Plano> OrganizaIndividuos(List<Plano> _Populacao, int _objetivo)
        {
            List<Plano> PopOrg = new List<Plano>();                      // População organizada por valor crescente do valor da função objetivo
            double[] valorObj = new double[_Populacao.Count()];          // Array para armazenar os valores da função objetivo
            int[] posicoes = new int[_Populacao.Count()];                // Array para conter as posições dos indivíduos da população a ser organizada

            // Compondo os arrays para organização segundo a receita média do produtor
            if(_objetivo == 1)
            {
                // Armazenando os valores de receita e suas posições nos respectivos arrays
                for(int i = 0; i < _Populacao.Count(); i++)
                {
                    valorObj[i] = -(_Populacao[i].receitaMediaProdutor); // Adicionando a i-ésima receita ao array
                    posicoes[i] = i;                                     // Adicionando a i-ésima posição ao array
                }
            }

            // Compondo os arrays para organização segundo o índice EENS
            else if(_objetivo == 2)
            {
                // Armazenando os valores de receita e suas posições nos respectivos arrays
                for(int i = 0; i < _Populacao.Count(); i++)
                {
                    valorObj[i] = _Populacao[i].eensPlano;             // Adicionando o i-ésimo custo ao array
                    posicoes[i] = i;                                   // Adicionando a i-ésima posição ao array
                }
            }

            // Compondo os arrays para organização segundo o custo médio de produção
            else if(_objetivo == 3)
            {
                // Armazenando os valores de receita e suas posições nos respectivos arrays
                for(int i = 0; i < _Populacao.Count(); i++)
                {
                    valorObj[i] = _Populacao[i].custoMedioProd;        // Adicionando o i-ésimo índice EENS ao array
                    posicoes[i] = i;                                   // Adicionando a i-ésima posição ao array
                }
            }

            // Organizando os arrays em ordem crescente
            Array.Sort(valorObj, posicoes);                            // Usando método para ordenar em ordem crescente o array de posições de acordo com o array de valores da função objetivo
            
            // Compondo a população em ordem crescente do valor da função objetivo
            for(int i = 0; i < _Populacao.Count(); i++)
            {
                PopOrg.Add(_Populacao[posicoes[i]]);                   // Adicionando i-ésimo indivíduo a população organizada em ordem crescente
            }

            return PopOrg;
        }

        #endregion

        // Método para criar roleta de seleção segundo um dos objetivos escolhidos
        #region Método para criar roleta de seleção segundo um dos objetivos

        public double[] CriarRoletaSelecao(int _numInd)
        {
            double[] roleta = new double[_numInd];                 // Array contendo os valores da roleta de seleção
            double denominador = 0;                                // Denominador geral

            // Loop para compor numerador do i-ésimo número e denominador geral
            for (int i = 0; i < _numInd; i++)
            {
                roleta[i] = _numInd - i;                           // Armazena valor do i-ésimo numerador
                denominador += roleta[i];                          // Somando i-ésimo valor ao denominador
            }
            
            // Cálculo do primeiro valor da roleta
            roleta[0] /= denominador;

            // Loop para cálculo da roleta para o i-ésimo indivíduo
            for (int i = 1; i < _numInd; i++)
            {
                roleta[i] = roleta[i - 1] + roleta[i]/denominador; // Calculando valor da roleta para o i-ésimo indivíduo
            }

            return roleta;
        }
        #endregion

        #endregion

        #endregion

        // Método para copiar populações
        #region Método para copiar populações
        public List<Plano> CopiarPopulacao(List<Plano> _Populacao)
        {
            List<Plano> PopulacaoCopia = new List<Plano>(); // População auxiliar para receber a população a ser copiada

            // Loop para cópia de cada indivíduo da população
            for (int i = 0; i < _Populacao.Count; i++)
            {
                PopulacaoCopia.Add(_Populacao[i]);          // Adiciona o i-ésimo indivíduo a população copiada
            }

            return PopulacaoCopia;                          // Retorna para a lista a população copiada
        }
        #endregion

        // Método para determinar indivíduos não dominados
        #region Método para classificação de dominância
        public int[] VerificaDominancia(List<Plano> _Populacao)
        {
            int numIndClass = _Populacao.Count;                                  // Número de indivíduos da população a ser classificada
            int [] classificacaoDominancia = new int[numIndClass];               // Array dizendo quantos indivíduos dominam um determinado indivíduo
            
            // Loop para verificar quantos indivíduos dominam o indivíduo atual
            for(int i = 0; i < _Populacao.Count; i++)
            {
                double objetivo11 = _Populacao[i].receitaMediaProdutor;          // Valor da função objetivo 1 do indivíduo atual
                double objetivo12 = _Populacao[i].eensPlano;                     // Valor da função objetivo 2 do indivíduo atual
                double objetivo13 = _Populacao[i].custoMedioProd;                // Valor da função objetivo 3 do indivíduo atual

                for(int j = 0; j < _Populacao.Count; j++)
                {
                    double objetivo21 = _Populacao[j].receitaMediaProdutor;      // Valor da função objetivo 1 de um indivíduo para comparação com o indivíduo atual
                    double objetivo22 = _Populacao[j].eensPlano;                 // Valor da função objetivo 2 de um indivíduo para comparação com o indivíduo atual
                    double objetivo23 = _Populacao[j].custoMedioProd;            // Valor da função objetivo 3 de um indivíduo para comparação com o indivíduo atual

                    // Caso o valor da função objetivo 1 (a ser maximizada) de um indivíduo seja maior que do indivíduo atual
                    if(objetivo21 > objetivo11 && objetivo22 <= objetivo12 && objetivo23 <= objetivo13) 
                    {
                        classificacaoDominancia[i]++;                            // Se a condição é verdadeira, aumenta o número de indivíduos que dominam o indivíduo atual
                    }
                    
                    // Caso o valor da função objetivo 2 (a ser minimizada) de um indivíduo seja menor que do indivíduo atual
                    else if(objetivo21 >= objetivo11 && objetivo22 < objetivo12 && objetivo23 <= objetivo13)
                    {
                        classificacaoDominancia[i]++;                            // Se a condição é verdadeira, aumenta o número de indivíduos que dominam o indivíduo atual
                    }

                    // Caso o valor da função objetivo 3 (a ser minimizada) de um indivíduo seja menor que do indivíduo atual
                    else if(objetivo21 >= objetivo11 && objetivo22 <= objetivo12 && objetivo23 < objetivo13)
                    {
                        classificacaoDominancia[i]++;                            // Se a condição é verdadeira, aumenta o número de indivíduos que dominam o indivíduo atual
                    }
                }
            }

            return classificacaoDominancia;                                      // Retorna para a variável o array contendo as posições de indivíduos não dominados
        }
        #endregion

        // Método para cálculo de Crowding Distance
        #region Método para cálculo de Crowding Distance
        public double[] CrowdingDistance(List<Plano> _Populacao)
        {
        
            int numIndPop = _Populacao.Count;                                                                 // Número de indivíduos na fronteira
            double[] _crowdingDistance = new double[numIndPop];                                               // Array para ser retornado com as Crowding Distances da fronteira de entrada

            double[] objetivo1 = new double[numIndPop];                                                       // Array para armazenar o valor da função objetivo 1 dos indivíduos da população
            double[] objetivo2 = new double[numIndPop];                                                       // Array para armazenar o valor da função objetivo 2 dos indivíduos da populaçã
            double[] objetivo3 = new double[numIndPop];                                                       // Array para armazenar o valor da função objetivo 2 dos indivíduos da populaçã

            int[] posicoes1 = new int[numIndPop];                                                             // Array para posições dos indivíduos segundo o primeiro objetivo
            int[] posicoes2 = new int[numIndPop];                                                             // Array para posições dos indivíduos segundo o segundo objetivo
            int[] posicoes3 = new int[numIndPop];                                                             // Array para posições dos indivíduos segundo o terceiro objetivo
            
            double min1;                                                                                      // Variável para armazenar o valor mínimo do primeiro objetivo
            double min2;                                                                                      // Variável para armazenar o valor mínimo do segundo objetivo
            double min3;                                                                                      // Variável para armazenar o valor mínimo do terceiro objetivo
            double max1;                                                                                      // Variável para armazenar o valor máximo do primeiro objetivo
            double max2;                                                                                      // Variável para armazenar o valor máximo do segundo objetivo
            double max3;                                                                                      // Variável para armazenar o valor máximo do terceiro objetivo


            // Loop para compor os arrays com os valores das funções objetivo e suas posições
            for(int i = 0; i < numIndPop; i++)
            {
                objetivo1[i] = _Populacao[i].receitaMediaProdutor;                                            // Armazena o valor do primeiro objetivo do i-ésimo indivíduo
                objetivo2[i] = _Populacao[i].eensPlano;                                                       // Armazena o valor do segundo objetivo do i-ésimo indivíduo
                objetivo3[i] = _Populacao[i].custoMedioProd;                                                  // Armazena o valor do terceiro objetivo do i-ésimo indivíduo

                posicoes1[i] = i;                                                                             // Armazena a posição do i-ésimo indivíduo no primeiro array
                posicoes2[i] = i;                                                                             // Armazena a posição do i-ésimo indivíduo no segundo array
                posicoes3[i] = i;                                                                             // Armazena a posição do i-ésimo indivíduo no terceiro array
            }

            // Organizando arrays de posições de acordo com os valores das funções objetivo em ordem crescente
            Array.Sort(objetivo1, posicoes1);
            Array.Sort(objetivo2, posicoes2);
            Array.Sort(objetivo3, posicoes3);

            // Determinando melhores e piores valores para cada função objetivo
            min1 = objetivo1[0];                                                                              // Guardando menor valor para o primeiro objetivo
            min2 = objetivo2[0];                                                                              // Guardando menor valor para o segundo objetivo
            min3 = objetivo3[0];                                                                              // Guardando menor valor para o terceiro objetivo

            max1 = objetivo1[numIndPop - 1];                                                                  // Guardando maior valor para o primeiro objetivo
            max2 = objetivo2[numIndPop - 1];                                                                  // Guardando mario valor para o segundo objetivo
            max3 = objetivo3[numIndPop - 1];                                                                  // Guardando mario valor para o terceiro objetivo

            // Loop para compor array com Crowding Distance de cada indivíduo
            for(int i = 0; i < numIndPop; i++)
            {
                // Para os indivíduos extremos de cada função objetivo
                if(i == 0 || i == (numIndPop - 1))
                {
                    _crowdingDistance[posicoes1[i]] += double.MaxValue;
                    _crowdingDistance[posicoes2[i]] += double.MaxValue;
                    _crowdingDistance[posicoes3[i]] += double.MaxValue;
                }
                // Para os demais indivíduos
                else
                {
                    _crowdingDistance[posicoes1[i]] += (objetivo1[i + 1] - objetivo1[i - 1]) / (max1 - min1);
                    _crowdingDistance[posicoes2[i]] += (objetivo2[i + 1] - objetivo2[i - 1]) / (max2 - min2);
                    _crowdingDistance[posicoes3[i]] += (objetivo3[i + 1] - objetivo3[i - 1]) / (max3 - min3);
                }
            }

            return _crowdingDistance;
        }

        #endregion

        // Método para sortear indivíduos
        #region Método para seleção de indivíduos por torneio binário

        public Plano SortearIndividuos(List<Plano> _Populacao, int[] _fronteiraDom, double[] _crowdingDistance)
        {
            Plano individuo = new Plano(sistema);  // Plano para receber o candidato a segundo pai
            int tamanhoPop = _Populacao.Count - 1; // Variável contendo o tamanho da população
            int numFron = _fronteiraDom.Length;    // Variável para determinar o número de fronteiras existentes
            int x;                                 // Variável para sortear o primeiro indivíduo a concorrer a ser o segundo pai
            int y;                                 // Variável para sortear o segundo indivíduo a concorrer a ser o segundo pai
            int fronX = new int();                 // Variável para determinar em qual fronteira está o primeiro concorrente a segundo pai
            int fronY = new int();                 // Variável para determinar em qual fronteira está o segundo concorrente a segundo pai
            
            x = rd.Next(0, tamanhoPop);            // Sorteia o primeiro indivíduo a concorrer a ser o segundo pai
            y = rd.Next(0, tamanhoPop);            // Sorteia o segundo indivíduo a concorrer a ser o segundo pai
            
            // Vendo em qual fronteira estão os indivíduos sorteados para concorrer a ser o segundo pai
            for(int i = (numFron - 1); i >= 0 ; i--)
            {
                // Determinando fronteira do primeiro indivíduo
                if(x < _fronteiraDom[i])
                {
                    fronX = i;                     // Armazena a fronteira em que o indivíduo se encontra
                }
                
                // Determinando fronteira do segundo indivíduo
                if(y < _fronteiraDom[i])
                {
                    fronY = i;                     // Armazena a fronteira em que o indivíduo se encontra
                }
            }
            
            // Escolhendo qual indivíduo deve ir para a população de pais
            // Caso o primeiro indivíduo seja de uma fronteira menos dominada que o segundo
            if (fronX < fronY)
            {
                individuo = _Populacao[x];         // O indivíduo X se torna o primeiro candidato a ser pai
            }
            
            // Caso o segundo indivíduo seja de uma fronteira menos dominada que o primeiro
            else if (fronY < fronX)
            {
                individuo = _Populacao[y];         // O indivíduo Y se torna o primeiro candidato a ser pai
            }
            
            // Caso ambos os indivíduos estejam na mesma fronteira
            else if(fronX == fronY)
            {
                // Escolhendo indivíduo de maior Crowding Distance
                // Caso o primeiro indivíduo tenha maior Crowding Distance que o segundo
                if(_crowdingDistance[x] > _crowdingDistance[y])
                {
                    individuo = _Populacao[x];     // O indivíduo X se torna o primeiro candidato a ser pai
                }
                
                // Caso o primeiro indivíduo tenha maior Crowding Distance que o segundo
                else if(_crowdingDistance[y] > _crowdingDistance[x])
                {
                    individuo = _Populacao[y];     // O indivíduo Y se torna o primeiro candidato a ser pai
                }
                
                // Caso ambas Crowding Distances sejam iguais
                else if(_crowdingDistance[x] == _crowdingDistance[y])
                {
                    individuo = _Populacao[x];     // Qualquer um dos indivíduos pode ser o candidato a ser pai
                }
            }
            
            return individuo;                      // Retornando indivíduo selecionado
        }

        #endregion

        // Método para mutar um indivíduo
        #region Método para mutação de um indivíduo

        public Plano MutacaoIndividuo (Plano _individuo)
        {
            Plano individuoMut = new Plano(sistema);                                           // Indivíduo que será mutado ao longo do método
            List<Gerador> UGsMan = this.sistema.GeradoresMnt;                                  // Lista com os geradores que devem ser programados para a manunteção
            string _perEst = this.sistema.testeAtual.periodoEst;                               // Variável que define o período de estudo
            double mutRate = this.taxaMut/100;                                                 // Variável contendo a taxa de mutação de cada gene
            int semanaIni = this.sistema.testeAtual.semIniMnt;                                 // Semana de início do período de estudo
            int semanaFim = this.sistema.testeAtual.semFimMnt;                                 // Semana do fim do período de estudo
            int _semMax;                                                                       // Semana máxima em que a manutenção pode ocorrer no período de estudo
            int _numGenes = _individuo.vetorPlano.Length;                                      // Determinando número de genes do indivíduo
            int sinMut = new int();                                                            // Variável para receber o sinal da mutação
            int intMut;                                                                        // Variável para a intensidade da mutação
            double sinSort;                                                                    // Variável para receber o sorteio do sinal da mutação
            double numSorteado;                                                                // Variável para receber um número de 0 a 1 a ser multiplicado pela intensidade da mutação
            

            // Copiando os genes do indivíduo a ser mutado
            for(int i = 0; i < _numGenes; i++)
            {
                individuoMut.vetorPlano[i] = _individuo.vetorPlano[i];                         // Copia o gene para o indivíduo a sofrer o processo de mutação
            }

            // Loop para mutação dos genes do indivíduo selecionado
            for(int i = 0; i < _numGenes; i++)
            {
                numSorteado = rd.NextDouble();                                                 // Sorteando número para determinar se o gene deve ou não ser mutdao

                // Caso o número sorteado seja menor que a taxa de mutação
                if(numSorteado <= mutRate)
                {
                    sinSort = rd.NextDouble();                                                 // Sorteando o sinal da mutação
                    intMut = 1;                                                                // Sorteando a intensidade da mutação

                    //Determinando o sinal da mutação
                    if(sinSort >= 0 && sinSort <= 0.5)
                    {
                        sinMut = -1;                                                           // Caso o valor sorteado esteja nesse intervalo, será negativo
                    }

                    else if(sinSort > 0.5 && sinSort <= 1)
                    {
                        sinMut = 1;                                                            // Caso o valor sorteado esteja nesse intervalo, será positivo
                    }

                    individuoMut.vetorPlano[i] = individuoMut.vetorPlano[i] + sinMut * intMut; // O gene sorteado é mutado

                    _semMax = semanaFim - UGsMan[i].nSemNecMnt + 1;
                    
                    // Verificando se a mutação não extrapola os limites do período de estudo
                    if(individuoMut.vetorPlano[i] < semanaIni)
                    {
                        individuoMut.vetorPlano[i] = semanaIni;                            // Caso extrapole o limite inferior, o gene receberá seu valor
                    }
                        
                    if(individuoMut.vetorPlano[i] > _semMax)
                    {
                        individuoMut.vetorPlano[i] = _semMax;                              // Caso extrapole o limite superior, o gene receberá seu valor
                    }
                    
                }
            }

            return individuoMut;
        }

        #endregion

        // Método para selecionar indivíduos para a próxima geração
        #region Método para selecionar indivíduos para a próxima geração

        public List<Plano> SelecaoIndividuos(List<Plano> _Populacao, int[] _frontDom, double[] _crowdingDistance)
        {
            List<Plano> PopulacaoCD = new List<Plano>(); // População rearranjada por ordem decrescente da Crowding Distance
            List<Plano> ProxGer = new List<Plano>();     // População de pais selecionados para a próxima geração
            int tamanhoPop = _Populacao.Count;           // Número de indivíduos da população de pais e filhos
            int numFron = _frontDom.Length;              // Número de fronteiras de dominância
            List<int> Posicoes = new List<int>();        // Lista contendo posições dos indivíduos da fronteira atual
            List<double> CrowDist = new List<double>();  // Lista contendo Crowding Distances dos indivíduos da fronteira atual
            int[] vetorPos;                              // Array para receber as posições dos indivíduos da fronteira atual
            double[] vetorCrowDist;                      // Array para receber as Croding Distances dos indivíduos da fronteira atual
            int contInd = 0;

            // Loop para ordenar a população por ordem crescente de Crowding Distance
            for(int i = 0; i < numFron; i++)
            {
                
                // Loop para obter Crowding Distance e posições da fronteira atual
                while(contInd < _frontDom[i])
                {
                    Posicoes.Add(contInd);                    // Adiciona a lista as posições dos indivíduos da fronteira atual
                    CrowDist.Add(_crowdingDistance[contInd]); // Adiciona a lista as Crowding Distances dos indivíduos da fronteira atual
                    contInd++;                                // Aumenta o contador de indivíduos em uma unidade
                }

                int numIndFron = Posicoes.Count;              // Contando o número de indivíduos da fronteira atual

                vetorPos = new int[numIndFron];               // Instanciando array de posições com o tamanho necessário
                vetorCrowDist = new double[numIndFron];       // Instanciando array de Crowding Distances com o tamanho necessário

                // Convertendo lista em array para ordenação
                for(int j = 0; j < numIndFron; j++)
                {
                    vetorPos[j] = Posicoes[j];                // Convertendo a lista com as posições em um array para ordenação
                    vetorCrowDist[j] = CrowDist[j];           // Convertendo a lista com as Crowding Distances em um array para ordenação
                }

                Array.Sort(vetorCrowDist, vetorPos);          // Ordenando, em ordem crescente, o array com as posições de acordo com o array de Crowding Distances
                Array.Reverse(vetorPos);                      // Invertendo o array para ficar em ordem decrescente

                for(int j = 0; j < numIndFron; j++)
                {
                    PopulacaoCD.Add(_Populacao[vetorPos[j]]); // Adicionando os indivíduos com maior Crowding Distance primeiro a população
                }

                Posicoes.Clear();                             // Limpando lista com posições para receber a próxima fronteira
                CrowDist.Clear();                             // Limpando lista com Crowding Distances para receber a próxima fronteira
            }

            // Adicionando os indivíduos apenas até que seja atingido o número necessário
            for(int i = 0; i < (tamanhoPop/2); i++)
            {
                ProxGer.Add(PopulacaoCD[i]);                  // Adiciona o i-ésimo indivíduo aos pais da próxima geração
            }

            return ProxGer;
        }

        #endregion

        // Método para determinar as dez melhores soluções para cada objetivo
        #region Método para seleção das dez melhores soluções para cada objetivo

        public List<Plano> MelhoresSolucoes(List<Plano> _TopSolutions, List<Plano> _Populacao)
        {
            // Instanciando algumas variáveis necessárias
            List<Plano> SolucoesNaoDom = new List<Plano>();                                // Lista para conter apena os indivíduos não dominados da população
            List<Plano> MelhoresSolucoes = new List<Plano>();                              // Lista contendo as melhores soluções da geração atual
            List<Plano> AuxMelhoresSolucoes = new List<Plano>();                           // Lista auxiliar contendo todas as melhores soluções
            List<Plano> MelhoresSolucoes1 = new List<Plano>();                             // Lista contendo as melhores soluções para o primeiro objetivo
            List<Plano> MelhoresSolucoes2 = new List<Plano>();                             // Lista contendo as melhores soluções para o segundo objetivo
            List<Plano> MelhoresSolucoes3 = new List<Plano>();                             // Lista contendo as melhores soluções para o terceiro objetivo
            List<Plano> MelhoresSolucoes4 = new List<Plano>();                             // Lista contendo as melhores soluções gerais
            List<Plano> AuxMelhoresSolucoes1 = new List<Plano>();                          // Lista auxiliar contendo todas as melhores soluções para o primeiro objetivo
            List<Plano> AuxMelhoresSolucoes2 = new List<Plano>();                          // Lista auxiliar contendo todas as melhores soluções para o segundi objetivo
            List<Plano> AuxMelhoresSolucoes3 = new List<Plano>();                          // Lista auxiliar contendo todas as melhores soluções para o terceiro objetivo
            List<Plano> AuxMelhoresSolucoes4 = new List<Plano>();                          // Lista auxiliar contendo as melhores soluções gerais
            List<int> ListaIndRep = new List<int>();                                       // Lista contendo os indivíduos da geração atual que são iguais a indivíduos das melhores soluções da geração anterior
            int contInd;                                                                   // Contador de indivíduos selecionados dentre as soluções não dominadas
            int tamPop = _Populacao.Count();                                               // Tamanho da população de entrada
            int numSolAnt = _TopSolutions.Count() / 4;                                     // Número de soluções dentro da lista de melhores soluções da geração anterior
            int numSolAt;                                                                  // Número de soluções dentro da lista de melhores soluções da geração atual
            int numGer = _Populacao[0].vetorPlano.Length;                                  // Número de variáveis dentro do plano de manutenção
            int [] frontDom = new int[tamPop];                                             // Array contendo a fronteira em que cada indivíduo se encontra
            int [] indIguais = new int[2];                                                 // Array contendo o par de indivíduos iguais entre as melhores soluções e a população da geração atual
            int [] posicoes1;                                                              // Array contendo as posições dos indivíduos não dominados para serem organizados segundo o primeiro objetivo
            int [] posicoes2;                                                              // Array contendo as posições dos indivíduos não dominados para serem organizados segundo o segundo objetivo
            int [] posicoes3;                                                              // Array contendo as posições dos indivíduos não dominados para serem organizados segundo o terceiro objetivo
            int [] posicoes4;                                                              // Array contendo as posições dos indivíduos não dominados para serem organizados segundo melhores soluções gerais
            double [] valorObj1;                                                           // Array contendo os valores do primeiro objetivo
            double [] valorObj2;                                                           // Array contendo os valores do segundo objetivo
            double [] valorObj3;                                                           // Array contendo os valores do terceiro objetivo
            double [] arestasCubo = new double[3];                                         // Array para conter valor das arestas do i-ésimo indivíduo não dominado
            double [] volumeInd;                                                           // Array para conter o volume dos indivíduos não dominados
            double melhorObj1;                                                             // Variável para armazenar melhor valor para o primeiro objetivo
            double melhorObj2;                                                             // Variável para armazenar melhor valor para o segundo objetivo
            double melhorObj3;                                                             // Variável para armazenar melhor valor para o terceiro objetivo
            bool boolIndIguais;                                                            // Variável booleana para dizer se dois indivíduos são iguais

            Plano indCasoBase = new Plano(sistema);                                        // Objeto para receber caso base
            indCasoBase.AvaliaPlano(SMCNS);                                                // Executando avaliação de confiabilidade do caso base

            frontDom = VerificaDominancia(_Populacao);                                     // Utilizando o método para classificar a população em fronteiras de dominância

            // Loop para selecionar os indivíduos não dominados
            for(int i = 0; i < tamPop; i++)
            {
                // Adicionando apenas os indivíduos não dominados a lista de soluções não dominadas
                if(frontDom[i] == 0)
                {
                    SolucoesNaoDom.Add(_Populacao[i]);                                     // Adiciona o i-ésimo indivíduo a população
                }
            }

            // Determinando tamanho dos arrays
            posicoes1 = new int[SolucoesNaoDom.Count()];
            posicoes2 = new int[SolucoesNaoDom.Count()];
            posicoes3 = new int[SolucoesNaoDom.Count()];
            valorObj1 = new double[SolucoesNaoDom.Count()];
            valorObj2 = new double[SolucoesNaoDom.Count()];
            valorObj3 = new double[SolucoesNaoDom.Count()];

            // Compondo os arrays
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                posicoes1[i] = i;                                                          // Adiciona posição ao primeiro array
                posicoes2[i] = i;                                                          // Adiciona posição ao segundo array
                posicoes3[i] = i;                                                          // Adiciona posição ao terceiro array
                valorObj1[i] = SolucoesNaoDom[i].receitaMediaProdutor;                     // Adiciona valor da primeira função objetivo ao array
                valorObj2[i] = SolucoesNaoDom[i].eensPlano;                                // Adiciona valor da segunda função objetivo ao array
                valorObj3[i] = SolucoesNaoDom[i].custoMedioProd;                           // Adiciona valor da terceira função objetivo ao array
            }

            Array.Sort(valorObj1, posicoes1);                                              // Ordenando array de posições segundo a ordem crescente de receita média do produtor
            Array.Reverse(posicoes1);                                                      // Invertendo o array para que ele fique em ordem decrescente

            Array.Sort(valorObj2, posicoes2);                                              // Ordenando array de posições segundo a ordem crescente do índice EENS

            Array.Sort(valorObj3, posicoes3);                                              // Ordenando array de posições segundo a ordem crescente do custo médio de produção

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores dez indivíduos para a receita média do produtor na lista
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                MelhoresSolucoes1.Add(SolucoesNaoDom[posicoes1[i]]);                       // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores dez indivíduos para o índice EENS na lista
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                MelhoresSolucoes2.Add(SolucoesNaoDom[posicoes2[i]]);                       // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores dez indivíduos para o custo médio de produção na lista
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                MelhoresSolucoes3.Add(SolucoesNaoDom[posicoes3[i]]);                       // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            numSolAt = MelhoresSolucoes1.Count();                                          // Determinando número de melhores soluções na geração atual

            // Comparando melhores indivíduos para a primeira função objetivo entre a população anterior e a atual para ver se existem indivíduos iguais
            for(int i = 0; i < numSolAnt; i++)
            {
                for(int j = 0; j < numSolAt; j++)
                {
                    boolIndIguais = true;                                                  // Reiniciando variável booelana

                    // Comparando cada posição do vetor plano
                    for( int k = 0; k < numGer; k++)
                    {
                        // Caso alguma dos valores dos vetorres plano sejam diferentes
                        if(_TopSolutions[i].vetorPlano[k] != MelhoresSolucoes1[j].vetorPlano[k])
                        {
                            boolIndIguais = false;                                         // Indivíduos não são iguais
                            break;                                                         // Interrompe loop for para comparação dos vetores plano
                        }
                    }

                    // Caso os indivíduos comparados sejam iguais
                    if(boolIndIguais == true)
                    {
                        ListaIndRep.Add(j);                                                // Inclui posição do indivíduo repetido a lista
                    }
                }
            }

            AuxMelhoresSolucoes1 = CopiarPopulacao(MelhoresSolucoes1);                     // Copiando lista para lista auxiliar

            // Removendo indivíduos repetidos para as populações com melhores valores para o primeiro objetivo
            for(int i = 0; i < ListaIndRep.Count(); i++)
            {
                MelhoresSolucoes1.Remove(AuxMelhoresSolucoes1[ListaIndRep[i]]);            // Remove i-ésimo indivíduo repetido
            }

            ListaIndRep.Clear(); // Limpando lista para recebimento dos próximos indivíduos repetidos

            // Comparando melhores indivíduos para a segunda função objetivo entre a população anterior e a atual para ver se existem indivíduos iguais
            for(int i = numSolAnt; i < 2 * numSolAnt; i++)
            {
                for(int j = 0; j < numSolAt; j++)
                {
                    boolIndIguais = true;                                                  // Reiniciando variável booelana

                    // Comparando cada posição do vetor plano
                    for( int k = 0; k < numGer; k++)
                    {
                        // Caso alguma dos valores dos vetorres plano sejam diferentes
                        if(_TopSolutions[i].vetorPlano[k] != MelhoresSolucoes2[j].vetorPlano[k])
                        {
                            boolIndIguais = false;                                         // Indivíduos não são iguais
                            break;                                                         // Interrompe loop for para comparação dos vetores plano
                        }
                    }

                    // Caso os indivíduos comparados sejam iguais
                    if(boolIndIguais == true)
                    {
                        ListaIndRep.Add(j);                                                // Inclui posição do indivíduo repetido a lista
                    }
                }
            }

            AuxMelhoresSolucoes2 = CopiarPopulacao(MelhoresSolucoes2);                     // Copiando lista para lista auxiliar

            // Removendo indivíduos repetidos para as populações com melhores valores para o primeiro objetivo
            for(int i = 0; i < ListaIndRep.Count(); i++)
            {
                MelhoresSolucoes2.Remove(AuxMelhoresSolucoes2[ListaIndRep[i]]);            // Remove i-ésimo indivíduo repetido
            }

            ListaIndRep.Clear(); // Limpando lista para recebimento dos próximos indivíduos repetidos

            // Comparando melhores indivíduos para a terceira função objetivo entre a população anterior e a atual para ver se existem indivíduos iguais
            for(int i = 2 * numSolAnt; i < 3 * numSolAnt; i++)
            {
                for(int j = 0; j < numSolAt; j++)
                {
                    boolIndIguais = true;                                                  // Reiniciando variável booelana

                    // Comparando cada posição do vetor plano
                    for( int k = 0; k < numGer; k++)
                    {
                        // Caso alguma dos valores dos vetorres plano sejam diferentes
                        if(_TopSolutions[i].vetorPlano[k] != MelhoresSolucoes3[j].vetorPlano[k])
                        {
                            boolIndIguais = false;                                         // Indivíduos não são iguais
                            break;                                                         // Interrompe loop for para comparação dos vetores plano
                        }
                    }

                    // Caso os indivíduos comparados sejam iguais
                    if(boolIndIguais == true)
                    {
                        ListaIndRep.Add(j);                                                // Inclui posição do indivíduo repetido a lista
                    }
                }
            }

            AuxMelhoresSolucoes3 = CopiarPopulacao(MelhoresSolucoes3);                     // Copiando lista para lista auxiliar

            // Removendo indivíduos repetidos para as populações com melhores valores para o primeiro objetivo
            for(int i = 0; i < ListaIndRep.Count(); i++)
            {
                MelhoresSolucoes3.Remove(AuxMelhoresSolucoes3[ListaIndRep[i]]);            // Remove i-ésimo indivíduo repetido
            }

            AuxMelhoresSolucoes1.Clear();                                                  // Limpando lista auxiliar
            AuxMelhoresSolucoes2.Clear();                                                  // Limpando lista auxiliar
            AuxMelhoresSolucoes3.Clear();                                                  // Limpando lista auxiliar

            // Adicionando indivíduos das melhores soluções atuais para o primeiro objetivo a população auxiliar
            for(int i = 0; i < MelhoresSolucoes1.Count(); i++)
            {
                AuxMelhoresSolucoes1.Add(MelhoresSolucoes1[i]);                            // Adiciona i-ésimo indivíduo
            }

            // Adicionando indivíduos das melhores soluções atuais para o segundo objetivo a população auxiliar
            for(int i = 0; i < MelhoresSolucoes2.Count(); i++)
            {
                AuxMelhoresSolucoes2.Add(MelhoresSolucoes2[i]);                            // Adiciona i-ésimo indivíduo
            }

            // Adicionando indivíduos das melhores soluções atuais para o terceiro objetivo a população auxiliar
            for(int i = 0; i < MelhoresSolucoes3.Count(); i++)
            {
                AuxMelhoresSolucoes3.Add(MelhoresSolucoes3[i]);                            // Adiciona i-ésimo indivíduo
            }

            // Adicionando indivíduos das melhores soluções anteriores para o primeiro objetivo a população auxiliar
            for(int i = 0; i < numSolAnt; i ++)
            {
                AuxMelhoresSolucoes1.Add(_TopSolutions[i]);                                // Adiciona i-ésimo indivíduo
            }

            // Adicionando indivíduos das melhores soluções anteriores para o segundo objetivo a população auxiliar
            for(int i = numSolAnt; i < 2 * numSolAnt; i ++)
            {
                AuxMelhoresSolucoes2.Add(_TopSolutions[i]);                                // Adiciona i-ésimo indivíduo
            }

            // Adicionando indivíduos das melhores soluções anteriores para o terceiro objetivo a população auxiliar
            for(int i = 2 * numSolAnt; i < 3 * numSolAnt; i ++)
            {
                AuxMelhoresSolucoes3.Add(_TopSolutions[i]);                                // Adiciona i-ésimo indivíduo
            }

            // Determinando melhores soluções para o primeiro objetivo
            frontDom = new int[AuxMelhoresSolucoes1.Count()];                              // Reiniciando array contendo as fronteiras de dominância
            frontDom = VerificaDominancia(AuxMelhoresSolucoes1);                           // Utilizando método para classificar os indivíduos da lista em fronteiras de dominância

            MelhoresSolucoes1.Clear();                                                     // Limpando lista com melhores soluções para o primeiro objetivo da geração atual

            // Loop para compor lista com as melhores soluções
            for(int i = 0; i < AuxMelhoresSolucoes1.Count(); i++)
            {
                // Caso o indivíduo não seja dominado
                if(frontDom[i] == 0)
                {
                    MelhoresSolucoes1.Add(AuxMelhoresSolucoes1[i]);                        // Adiciona i-ésimo indivíduo a lista
                }
            }

            AuxMelhoresSolucoes1.Clear();                                                  // Limpando lista auxiliar para o primeiro objetivo
            AuxMelhoresSolucoes1 = CopiarPopulacao(MelhoresSolucoes1);                     // Copiando lista para lista auxiliar

            posicoes1 = new int[AuxMelhoresSolucoes1.Count()];                             // Reiniciando array para posições dos melhores indivíduos segundo o primeiro objetivo
            valorObj1 = new double[AuxMelhoresSolucoes1.Count()];                          // Reiniciando array para valores dos melhores indivíduos segundo o primeiro objetivo
            
            for(int i = 0; i < AuxMelhoresSolucoes1.Count(); i++)
            {
                posicoes1[i] = i;                                                          // Adiciona posição ao primeiro array
                valorObj1[i] = AuxMelhoresSolucoes1[i].receitaMediaProdutor;               // Adiciona valor da primeira função objetivo ao array
            }

            Array.Sort(valorObj1, posicoes1);                                              // Ordenando array de posições segundo a ordem crescente de receita média do produtor
            Array.Reverse(posicoes1);                                                      // Invertendo o array para que ele fique em ordem decrescente

            // Determinando melhores soluções para o segundo objetivo
            frontDom = new int[AuxMelhoresSolucoes2.Count()];                              // Reiniciando array contendo as fronteiras de dominância
            frontDom = VerificaDominancia(AuxMelhoresSolucoes2);                           // Utilizando método para classificar os indivíduos da lista em fronteiras de dominância

            MelhoresSolucoes2.Clear();                                                     // Limpando lista com melhores soluções para o segundo objetivo da geração atual

            // Loop para compor lista com as melhores soluções
            for(int i = 0; i < AuxMelhoresSolucoes2.Count(); i++)
            {
                // Caso o indivíduo não seja dominado
                if(frontDom[i] == 0)
                {
                    MelhoresSolucoes2.Add(AuxMelhoresSolucoes2[i]);                        // Adiciona i-ésimo indivíduo a lista
                }
            }

            AuxMelhoresSolucoes2.Clear();                                                  // Limpando lista auxiliar para o primeiro objetivo
            AuxMelhoresSolucoes2 = CopiarPopulacao(MelhoresSolucoes2);                     // Copiando lista para lista auxiliar
            
            posicoes2 = new int[AuxMelhoresSolucoes2.Count()];                             // Reiniciando array para posições dos melhores indivíduos segundo o primeiro objetivo
            valorObj2 = new double[AuxMelhoresSolucoes2.Count()];                          // Reiniciando array para valores dos melhores indivíduos segundo o primeiro objetivo
            
            for(int i = 0; i < AuxMelhoresSolucoes2.Count(); i++)
            {
                posicoes2[i] = i;                                                          // Adiciona posição ao primeiro array
                valorObj2[i] = AuxMelhoresSolucoes2[i].eensPlano;                          // Adiciona valor da primeira função objetivo ao array
            }

            Array.Sort(valorObj2, posicoes2);                                              // Ordenando array de posições segundo a ordem crescente de receita média do produtor
            
            // Determinando melhores soluções para o terceiro objetivo
            frontDom = new int[AuxMelhoresSolucoes3.Count()];                              // Reiniciando array contendo as fronteiras de dominância
            frontDom = VerificaDominancia(AuxMelhoresSolucoes3);                           // Utilizando método para classificar os indivíduos da lista em fronteiras de dominância

            MelhoresSolucoes3.Clear();                                                     // Limpando lista com melhores soluções para o terceiro objetivo da geração atual

            // Loop para compor lista com as melhores soluções
            for(int i = 0; i < AuxMelhoresSolucoes3.Count(); i++)
            {
                // Caso o indivíduo não seja dominado
                if(frontDom[i] == 0)
                {
                    MelhoresSolucoes3.Add(AuxMelhoresSolucoes3[i]);                        // Adiciona i-ésimo indivíduo a lista
                }
            }

            AuxMelhoresSolucoes3.Clear();                                                  // Limpando lista auxiliar para o primeiro objetivo
            AuxMelhoresSolucoes3 = CopiarPopulacao(MelhoresSolucoes3);                     // Copiando lista para lista auxiliar
            
            posicoes3 = new int[AuxMelhoresSolucoes3.Count()];                             // Reiniciando array para posições dos melhores indivíduos segundo o primeiro objetivo
            valorObj3 = new double[AuxMelhoresSolucoes3.Count()];                          // Reiniciando array para valores dos melhores indivíduos segundo o primeiro objetivo
            
            for(int i = 0; i < AuxMelhoresSolucoes3.Count(); i++)
            {
                posicoes3[i] = i;                                                          // Adiciona posição ao primeiro array
                valorObj3[i] = AuxMelhoresSolucoes3[i].custoMedioProd;                     // Adiciona valor da primeira função objetivo ao array
            }

            Array.Sort(valorObj3, posicoes3);                                              // Ordenando array de posições segundo a ordem crescente de receita média do produtor
            
            // Determinando número de soluções para cada objetivo da geração atual (para que não haja diferenças nos números de solução para cada objetivo)
            int [] numSol = {AuxMelhoresSolucoes1.Count(), AuxMelhoresSolucoes2.Count(), AuxMelhoresSolucoes3.Count()};
            numSolAt = numSol.Min();

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores indivíduos para a primeira função objetivo à lista
            for(int i = 0; i < numSolAt; i++)
            {
                MelhoresSolucoes.Add(AuxMelhoresSolucoes1[posicoes1[i]]);                  // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores indivíduos para a segunda função objetivo à lista
            for(int i = 0; i < numSolAt; i++)
            {
                MelhoresSolucoes.Add(AuxMelhoresSolucoes2[posicoes2[i]]);                  // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores indivíduos para a primeira função objetivo à lista
            for(int i = 0; i < numSolAt; i++)
            {
                MelhoresSolucoes.Add(AuxMelhoresSolucoes3[posicoes3[i]]);                  // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            // Determinando melhores valores para cada função objetivo
            melhorObj1 = 2 * indCasoBase.receitaMediaProdutor;                             // Armazena melhor valor para o primeiro objetivo
            melhorObj2 = indCasoBase.eensPlano;                                            // Armazena melhor valor para o segundo objetivo
            melhorObj3 = indCasoBase.custoMedioProd;                                       // Armazena melhor valor para o terceiro objetivo

            posicoes4 = new int[SolucoesNaoDom.Count()];                                   // Determinando tamanho do array para conter a posição de todos os indivíduos não dominados
            volumeInd = new double[SolucoesNaoDom.Count()];                                // Determinando tamanho do array para conter o volume de todos os indivíduos não dominados

            // Loop para determinar arestas do volume formado por cada indivíduo
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                arestasCubo[0] = melhorObj1 - SolucoesNaoDom[i].receitaMediaProdutor;      // Determinando aresta relativa ao terceiro objetivo
                arestasCubo[1] = SolucoesNaoDom[i].eensPlano - melhorObj2;                 // Determinando aresta relativa ao terceiro objetivo
                arestasCubo[2] = SolucoesNaoDom[i].custoMedioProd - melhorObj3;            // Determinando aresta relativa ao terceiro objetivo

                volumeInd[i] = Math.Abs(arestasCubo[0] * arestasCubo[1] * arestasCubo[2]); // Calculando volume para o i-ésimo indivíduo
                posicoes4[i] = i;                                                          // Armazenando posição do i-ésimo indivíduo no array
            }

            Array.Sort(volumeInd, posicoes4);                                              // Ordenando array de posições segundo a ordem crescente do custo médio de produção

            contInd = 0;

            // Adicionando os melhores dez indivíduos gerais na lista
            for(int i = 0; i < SolucoesNaoDom.Count(); i++)
            {
                MelhoresSolucoes4.Add(SolucoesNaoDom[posicoes4[i]]);                       // Adiciona i-ésimo melhor indivíduo à lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            ListaIndRep.Clear(); // Limpando lista para recebimento dos indivíduos repetidos

            // Comparando melhores indivíduos gerais entre a população anterior e a atual para ver se existem indivíduos iguais
            for(int i = 3 * numSolAnt; i < 4 * numSolAnt; i++)
            {
                for(int j = 0; j < MelhoresSolucoes4.Count(); j++)
                {
                    boolIndIguais = true;                                                  // Reiniciando variável booelana

                    // Comparando cada posição do vetor plano
                    for( int k = 0; k < numGer; k++)
                    {
                        // Caso alguma dos valores dos vetorres plano sejam diferentes
                        if(_TopSolutions[i].vetorPlano[k] != MelhoresSolucoes4[j].vetorPlano[k])
                        {
                            boolIndIguais = false;                                         // Indivíduos não são iguais
                            break;                                                         // Interrompe loop for para comparação dos vetores plano
                        }
                    }

                    // Caso os indivíduos comparados sejam iguais
                    if(boolIndIguais == true)
                    {
                        ListaIndRep.Add(j);                                                // Inclui posição do indivíduo repetido a lista
                    }
                }
            }

            AuxMelhoresSolucoes4 = CopiarPopulacao(MelhoresSolucoes4);                     // Copiando lista para lista auxiliar

            // Removendo indivíduos repetidos para as populações com melhores valores para o primeiro objetivo
            for(int i = 0; i < ListaIndRep.Count(); i++)
            {
                MelhoresSolucoes4.Remove(AuxMelhoresSolucoes4[ListaIndRep[i]]);            // Remove i-ésimo indivíduo repetido
            }

            // Adicionando melhores indivíduos anteriores a nova lista
            for(int i = 3 * numSolAnt; i < 4 * numSolAnt; i++)
            {
                MelhoresSolucoes4.Add(_TopSolutions[i]);
            }

            volumeInd = new double[MelhoresSolucoes4.Count()];                             // Determinando tamanho do array para conter o volume de todos os indivíduos não dominado
            posicoes4 = new int[MelhoresSolucoes4.Count()];

            // Loop para determinar arestas do volume formado por cada indivíduo
            for(int i = 0; i < MelhoresSolucoes4.Count(); i++)
            {
                arestasCubo[0] = melhorObj1 - MelhoresSolucoes4[i].receitaMediaProdutor;   // Determinando aresta relativa ao primeiro objetivo
                arestasCubo[1] = MelhoresSolucoes4[i].eensPlano - melhorObj2;              // Determinando aresta relativa ao segundo objetivo
                arestasCubo[2] = MelhoresSolucoes4[i].custoMedioProd - melhorObj3;         // Determinando aresta relativa ao terceiro objetivo

                volumeInd[i] = Math.Abs(arestasCubo[0] * arestasCubo[1] * arestasCubo[2]); // Calculando volume para o i-ésimo indivíduo
                posicoes4[i] = i;                                                          // Armazenando posição do i-ésimo indivíduo no array
            }

            Array.Sort(volumeInd, posicoes4);                                              // Ordenando array de posições segundo a ordem crescente do custo médio de produção

            contInd = 0;                                                                   // Reiniciando contador

            // Adicionando os melhores indivíduos gerais à lista
            for(int i = 0; i < numSolAt; i++)
            {
                MelhoresSolucoes.Add(MelhoresSolucoes4[posicoes4[i]]);                  // Adiciona i-ésimo melhor indivíduo a lista
                contInd++;                                                                 // Acrescendo o contador de indivíduos em uma unidade

                // Caso dez indivíduos já tenham sido selecionados
                if(contInd > 9)
                {
                    break;                                                                 // Interrompendo loop for
                }
            }

            return MelhoresSolucoes;
        }

        #endregion

        // Método para avaliação da existência de geradores programados para manutenção na mesma semana em uma usina
        #region Método para avaliação da existência de geradores programados para manutenção na mesma semana em uma usina
        public bool VerificaCronograma(Plano _individuo, int[] _arrayUsinas, int[] _arrayNSemNecMnt)
        {
            bool semanaIgual = false; // Variável booleana para dizer se há geradores programados para mesmo intervalo dentro de uma mesma usina

            // Loop para verificação do cronograma
            for(int i = 0; i < _arrayUsinas.Length; i++)
            {

                for(int j = i + 1; j < _arrayUsinas.Length; j++)
                {

                    // Verificando valores das posições do vetor manutenção para geradores de mesma usina
                    if(_arrayUsinas[i] == _arrayUsinas[j])
                    {
                        // Verificando se o i-ésimo geradoor está contido dentro do intervalo de manutenção do j-ésimo gerador
                        if(_individuo.vetorPlano[i] >= (_individuo.vetorPlano[j] - _arrayNSemNecMnt[i] + 1) && _individuo.vetorPlano[i] <= (_individuo.vetorPlano[j] + _arrayNSemNecMnt[j] - 1))
                        {
                            semanaIgual = true; // Variável booleana tem seu valor modificado
                            break;              // Saindo do loop caso algum dos geradores esteja programado para manutenção dentro do intervalo de manutenção de outro gerador da mesma usina
                        }
                    }
                }

                // Saindo do loop caso algum dos geradores esteja programado para manutenção dentro do intervalo de manutenção de outro gerador da mesma usina
                if(semanaIgual == true)
                {
                    break;
                }
            }

            return semanaIgual;
        }

        #endregion

        #endregion

    }
    # endregion
    // ============================================================================================

    // Classe para definição do plano de manutenção
    # region Classe plano de manutenção
    class Plano
    {
        // ------------------------------------------------
        // Atributos da Classe
        DadosSistema sistema;
        List<Gerador> geradores;                 // Lista de geradores (unidades geradoras) que devem entrar para manutenção
        public int[] vetorPlano;                 // Vetor plano (cronograma de manutenção) - Indivíduo da ferramenta de otimização
        public double receitaMediaProdutor = 0;  // Receita média do produtor com este plano (FUNÇÃO OBJETIVO 01 para maximizar)
        public double custoMedioProd = 0;        // Custo médio de produção do plano (FUNÇÃO OBJETIVO 03 para minimizar)
        public double aptidao;                   // Aptidão (valor da função objetivo) associada ao plano
        double UC;                               // Custo do corte de carga [$/MWh]

        public double lolpPlano;     // Valor do índice de confiabilidade LOLP associado ao plano (apresentado pelo sistema quando este plano é colocado em execução)
        public double lolePlano;     // Valor do índice de confiabilidade LOLE associado ao plano (apresentado pelo sistema quando este plano é colocado em execução)
        public double epnsPlano;     // Valor do índice de confiabilidade EPNS associado ao plano (apresentado pelo sistema quando este plano é colocado em execução)
        public double eensPlano;     // Valor do índice de confiabilidade EENS associado ao plano (apresentado pelo sistema quando este plano é colocado em execução) (FUNÇÃO OBJETIVO 02 para minimizar)
        public double lolcPlano;     // Valor do índice de confiabilidade LOLC associado ao plano (apresentado pelo sistema quando este plano é colocado em execução)

        public double[] lolpPlanoSemanal;     // Valores do índice de confiabilidade LOLP para cada semana do período de estudo associados ao plano
        public double[] lolePlanoSemanal;     // Valores do índice de confiabilidade LOLE para cada semana do período de estudo associados ao plano
        public double[] epnsPlanoSemanal;     // Valores do índice de confiabilidade EPNS para cada semana do período de estudo associados ao plano
        public double[] eensPlanoSemanal;     // Valores do índice de confiabilidade EENS para cada semana do período de estudo associados ao plano

        public double desvioPercentual;       // Variável para comparação dos resultados da NSGA-II

        int contGerProg = 0;                  // Contador de geradores programados no plano      

        public double incidencia = 1;         // Índice de incidência (para resultados finais)

        public int classeKmeans = -1;         // Classe a qual o plano pertence no emprego do Kmeans

        // ------------------------------------------------
        // Método construtor do plano
        public Plano(DadosSistema _sistema)
        {
            this.sistema = _sistema;

            this.geradores = new List<Gerador>();
            this.geradores.AddRange(_sistema.Geradores);
            this.vetorPlano = new int[_sistema.nGerProgMnt];

            lolpPlano = new double();
            lolePlano = new double();
            epnsPlano = new double();
            eensPlano = new double();

            int nSemPerEst = (_sistema.semFimMnt + 1) - (_sistema.semIniMnt);   // Número de semanas do período de estudo
            lolpPlanoSemanal = new double[nSemPerEst];
            lolePlanoSemanal = new double[nSemPerEst];
            epnsPlanoSemanal = new double[nSemPerEst];
            eensPlanoSemanal = new double[nSemPerEst];

            this.UC = sistema.testeAtual.UC;
        }

        // ------------------------------------------------
        // Método para avaliar plano
        public void AvaliaPlano(ConfSMCNS _SMCNS)
        {
            // ------------------------------------------------
            // Definindo hora de início e de fim da manutenção para cada gerador programado
            foreach (Gerador gerador in geradores)
            {
                if (gerador.progMnt == true && vetorPlano[gerador.posvetorPlano] > 0)
                { // Se gerador deve ser programado para manutenção dentro do período de estudo e já foi programado
                    gerador.horaIniMnt = (vetorPlano[gerador.posvetorPlano] - 1) * 168 + (24 + 7);                        // Começando às 7h da segunda-feira da semana inicial de manutenção
                    gerador.horaFimMnt = (vetorPlano[gerador.posvetorPlano] + gerador.nSemNecMnt) * 168 - (24 + 7);       // Terminando às 17h da sexta-feira da última semana de manutenção
                }
                else if(gerador.progMnt == false && gerador.gerJaProgMnt == true)
                {// Se gerador já foi previamente programado para manutenção dentro do período de estudo
                    gerador.horaIniMnt = (gerador.semMntUGJaProgr - 1) * 168 + (24 + 7);                                  // Começando às 7h da segunda-feira da semana inicial de manutenção
                    gerador.horaFimMnt = (gerador.semMntUGJaProgr + gerador.nSemNecMnt) * 168 - (24 + 7);                 // Terminando às 17h da sexta-feira da última semana de manutenção
                }
                else
                { // Se gerador não deve ser programado para manutenção ou ainda não foi programado 
                    gerador.horaIniMnt = -1;
                    gerador.horaFimMnt = -1;
                }
            }

            // ------------------------------------------------
            // Ordenando, de forma crescente, semanas de início de manutenção de geradores de uma mesma usina
            // Para que avaliação de confiabilidade seja sempre a mesma
            OrdenaPlanoAvalConf();

            // ------------------------------------------------
            // Executa Avaliação de Confiabilidade
            _SMCNS.ExecutaSMCNS(vetorPlano);


            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            // Criando arquivo
            //TextWriter arqComparacao = new StreamWriter(new FileStream("aaaComparacao.txt", FileMode.Create));

            //arqComparacao.Write("Plano:");
            //for (int i = 0; i < vetorPlano.Length; i++)
            //{
            //    arqComparacao.Write("    {0:00}", vetorPlano[i]);
            //}
            //arqComparacao.WriteLine("");
            //arqComparacao.WriteLine("             {0}       {1}        {2}       {3}         {4}", "EgCost", "LOLC", "Aptidao", "LOLP", "EPNS");
            //arqComparacao.WriteLine(" CONV -    {0:00.00000}    {1:00.00000}    {2:00.00000}    {3:00.00000}    {4:00.00000}", _SMCNS.custoMedioProd / 1000000,
            //    _SMCNS.eens * UC / 1000000, _SMCNS.custoMedioProd / 1000000 + _SMCNS.eens * UC / 1000000, _SMCNS.lolp, _SMCNS.epns);
            //arqComparacao.WriteLine(" {0} estados sorteados para convergência", _SMCNS.NSAvaliacao);
            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

            //_SMCNS.ExecutaSMCNS_CE(vetorPlano);

            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            //arqComparacao.WriteLine("");
            //arqComparacao.WriteLine(" CE   -    {0:00.00000}    {1:00.00000}    {2:00.00000}    {3:00.00000}    {4:00.00000}", _SMCNS.custoMedioProd / 1000000,
            //    _SMCNS.eens * UC / 1000000, _SMCNS.custoMedioProd / 1000000 + _SMCNS.eens * UC / 1000000, _SMCNS.lolp, _SMCNS.epns);
            //arqComparacao.WriteLine(" {0} estados sorteados para convergência", _SMCNS.NSAvaliacao);
            //arqComparacao.Close();
            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

            this.lolpPlano = _SMCNS.lolp;
            this.lolePlano = _SMCNS.lole;
            this.epnsPlano = _SMCNS.epns;
            this.eensPlano = _SMCNS.eens;                                         // FUNÇÃO OBJETIVO 02 para minimizar
            this.lolePlanoSemanal = _SMCNS.lolePlanoSemanal;
            this.lolpPlanoSemanal = _SMCNS.lolpPlanoSemanal;
            this.lolcPlano = this.eensPlano * UC / 1000000;
            this.custoMedioProd = _SMCNS.custoMedioProd / 1000000;                // FUNÇÃO OBJETIVO 03 para minimizar
            this.receitaMediaProdutor = _SMCNS.receitaMediaProdutor / 1000000;    // FUNÇÃO OBJETIVO 01 para maximizar

            double somBonus = 0;
            //foreach (Gerador gerador in sistema.Geradores)
            //{ // Cálculo do sómatório dos bônus para função objetivo
            //    if (gerador.progMnt == true)
            //    {
            //        int aux = 0;
            //        if (vetorPlano[gerador.posvetorPlano] > 0) { aux = 1; }
            //        somBonus += gerador.bonusGer * aux;
            //        contGerProg += aux;
            //    }
            //}

            // Função objtivo do plano - Aptidão do indivíduo
            this.aptidao = this.custoMedioProd + this.lolcPlano - somBonus;
            // this.aptidao = -this.receitaMediaProdutor;
        }

        // Método para reescrever os dados do plano
        public override string ToString()
        {
            return "Periodo: " + sistema.periodoEst + " - Aptidao: " + aptidao.ToString("#00.00000") + " - GerProg: " + contGerProg.ToString("#00") + 
                " - ReceitaProd: " + (receitaMediaProdutor).ToString("#0.00000") + " - LOLE: " + (lolePlano).ToString("#0.00000") + " - EGcost: " + 
                (custoMedioProd).ToString("#0.00000");
        }

        // ------------------------------------------------
        // Método para ordenar, de forma crescente, semanas de início de manutenção de geradores de uma mesma usina
        // Isso é realizado para garantir que a avaliação de confiabilidade seja a mesma quando existem UGs iguais de uma mesma usina programadas em semanas diferentes
        # region Método de ordenação de geradores 
        public void OrdenaPlanoAvalConf()
        {
            int[] ordem;
            int contGer = 0;
            int contPosVetorPlano = 0;
            int[] vetorPlanoCopia = new int[sistema.nGerProgMnt];
            for (int i = 0; i < vetorPlano.Length; i++) { vetorPlanoCopia[i] = vetorPlano[i]; }
            for (int i = 0; i < sistema.nGerUsinMnt.Count(); i++)
            {
                if (sistema.nGerUsinMnt[i] <= 1)
                {
                    contPosVetorPlano++; contGer++; continue;
                }
                ordem = new int[sistema.nGerUsinMnt[i]];
                int[] semInicio = new int[sistema.nGerUsinMnt[i]];
                for (int j = 0; j < sistema.nGerUsinMnt[i]; j++)
                {
                    semInicio[j] = vetorPlano[contPosVetorPlano];
                    ordem[j] = contPosVetorPlano;
                    contPosVetorPlano++;
                }
                // Ordenando pela semana de início dentro da usina atual
                Array.Sort(semInicio, ordem);
                for (int j = 0; j < ordem.Length; j++)
                {
                    this.vetorPlano[contGer] = vetorPlanoCopia[ordem[j]];
                    contGer++;
                }
            }
        }
        # endregion
    }
    # endregion
}