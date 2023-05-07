using System;
using System.Linq;

namespace ProgMntUG
{
    // Classe para avaliação de confiabilidade - Simulação Monte Carlo Não-Sequencial (SMCNS)
    # region Classe da rotina SMCNS
    class ConfSMCNS
    {
        // ------------------------------------------------
        // Atributos da Classe
        DadosSistema sistema;
        Random rd;                                         // Objeto para geração de número aleatório
        int sementeSMCNS = 1234;                           // Semente para a SMCNS (a mesma para todas as avaliações)

        // ------------------------------------------------
        // Variáveis (atributos) relativas à SMCNS
        # region Variáveis da SMCNS
        double betaTol;                     // Tolerância para convergência
        int NSMAX = 10000000;               // Número máximo de estados
        Int64 NS = 0;                       // Número do estado atual
        double[] NSsemanal;                 // Número de estados sorteados dentro de cada semana do período de estudo
        double potGerDisp;                  // Capacidade de geração disponível no estado
        int nSemPerido;                     // Número de semanas dentro do período de estudo
        public double custoMedioProd = 0;   // Custo médio de produção do plano (FUNÇÃO OBJETIVO 03 para minimizar)
        public double receitaMediaProdutor = 0;  // Receita média do produtor com este plano (FUNÇÃO OBJETIVO 01 para maximizar)

        public Int64 NSAvaliacao;           // Número de estados sorteados para convergência do estado atual

        public double lolp = 0;    // índice LOLP para o período de estudo
        public double lole = 0;    // índice LOLE para o período de estudo
        public double epns = 0;    // índice EPNS para o período de estudo
        public double eens = 0;    // índice EENS para o período de estudo (FUNÇÃO OBJETIVO 02 para minimizar)

        double Vlolp = 0;   // Variância do índice LOLP
        double Blolp = 0;   // Coeficiente de variação do índice LOLP
        double Vepns = 0;   // Variância do índice EPNS
        double Bepns = 0;   // Coeficiente de variação do índice EPNS

        // Variáveis para valores acumulados dos índices do período
        double AFlolp = 0; double A2Flolp = 0;
        double AFepns = 0; double A2Fepns = 0;

        public double[] lolpPlanoSemanal;     // Valores do índice LOLP para cada semana do período de estudo
        public double[] lolePlanoSemanal;     // Valores do índice LOLE para cada semana do período de estudo
        public double[] epnsPlanoSemanal;     // Valores do índice EPNS para cada semana do período de estudo
        public double[] eensPlanoSemanal;     // Valores do índice EENS para cada semana do período de estudo

        // Variáveis para valores acumulados dos índices de cada semanda dentro do período
        double[] AFlolpPlanoSemanal;
        double[] AFepnsPlanoSemanal;
        # endregion

        // ------------------------------------------------
        // Método construtor da SMCNS
        public ConfSMCNS(DadosSistema _sistema)
        {
            this.sistema = _sistema;
            this.betaTol = _sistema.betaTol;

            this.nSemPerido = (_sistema.semFimMnt - _sistema.semIniMnt) + 1;
            lolpPlanoSemanal = new double[nSemPerido]; AFlolpPlanoSemanal = new double[nSemPerido];
            epnsPlanoSemanal = new double[nSemPerido]; AFepnsPlanoSemanal = new double[nSemPerido];
            lolePlanoSemanal = new double[nSemPerido];
            eensPlanoSemanal = new double[nSemPerido];

            NSsemanal = new double[nSemPerido];
        }

        // ------------------------------------------------
        // Método para zerar variáveis
        public void zeraVariaveis()
        {
            lolp = 0;    // índice LOLP para o período de estudo
            lole = 0;    // índice LOLE para o período de estudo
            epns = 0;    // índice EPNS para o período de estudo
            eens = 0;    // índice EENS para o período de estudo
            Vlolp = 0;   // Variância do índice LOLP
            Blolp = 0;   // Coeficiente de variação do índice LOLP
            Vepns = 0;   // Variância do índice EPNS
            Bepns = 0;   // Coeficiente de variação do índice EPNS
            AFlolp = 0; A2Flolp = 0;
            AFepns = 0; A2Fepns = 0;
            lolpPlanoSemanal = new double[nSemPerido]; AFlolpPlanoSemanal = new double[nSemPerido];
            epnsPlanoSemanal = new double[nSemPerido]; AFepnsPlanoSemanal = new double[nSemPerido];
            lolePlanoSemanal = new double[nSemPerido];
            eensPlanoSemanal = new double[nSemPerido];
            NS = 0;
            custoMedioProd = 0;

            foreach(Gerador gerador in sistema.GeradoresOrdCusto)
            { // Zerando receita dos geradores
                gerador.receitaMedia = 0;
            }
        }

        // ------------------------------------------------
        // Método de execução da SMCNS
        # region Método de execução da rotina SMCNS
        public void ExecutaSMCNS(int[] _vetorPlano)
        {
            zeraVariaveis();

            rd = new Random(sementeSMCNS);         // Instanciando objeto para obtenção de número aleatório
            foreach (Gerador gerador in sistema.Geradores)
            {
                gerador.eesSemanal = new double[nSemPerido];     // Instanciando o vetor que contém valores semanais de EES do gerador
            }

            // Atualiza número de avaliações de confiabilidade
            sistema.contAvalConf++;

            // ------------------------------------------------
            // Iterações da SMCNS
            while (NS < NSMAX)
            {
                potGerDisp = 0;       // Definindo potência geração disponível igual a zero para iniciar a avaliação do estado

                // ------------------------------------------------
                // Sorteio de hora dentro do período de estudo
                double y = rd.NextDouble();                                                // Sorteio de número pseudoaleatório para definir nível de carga
                int posHora = Convert.ToInt16((sistema.curvaCarga.Length - 1) * y);        // Posição da hora sorteada dentro da curva de carga
                int horaSort = posHora + 1;                                                // Hora sorteada dentro do período de estudo
                double carga = sistema.curvaCarga[posHora] * sistema.cargaTotal;           // Valor, em MW, da carga para a hora sorteada
                double cargaMaisReservaGer = sistema.curvaCarga[posHora] * (sistema.cargaTotal + sistema.testeAtual.reservaGer);     // Carga mais a reserva de geração a ser atendida
                double semanaSortDouble = (horaSort / 168.0) + 0.99999999;                 // Semana sorteada dentro do período de estudo
                int semanaSort = Convert.ToInt16(Math.Floor(semanaSortDouble));

                // ------------------------------------------------
                // Atualização da contagem de estados sorteados
                NS++;                          // Número total de estados sorteados            
                NSsemanal[semanaSort - 1]++;   // Número de estados sorteados dentro de cada semana

                // ------------------------------------------------
                // Verificação do estado operativo dos geradores
                foreach (Gerador gerador in sistema.Geradores)
                {
                    // Definindo gerador como disponível (para iniciair a avaliação)
                    gerador.estadoOperativo = 1;

                    // Definindo estado operativo do gerador de acordo com plano de manutenção para a hora sorteada
                    if (gerador.progMnt == true)
                    {
                        if (horaSort >= gerador.horaIniMnt && horaSort <= gerador.horaFimMnt)
                        { // Gerador está em manutenção na hora sorteada
                            gerador.estadoOperativo = 0;   // Definindo gerador como indisponível
                        }
                    }

                    // Definindo estado operativo do gerador de acordo com informação de programação previamente definido (se gerador já está previamente programado)
                    if (gerador.progMnt == false && gerador.gerJaProgMnt == true)
                    {
                        if (horaSort >= gerador.horaIniMnt && horaSort <= gerador.horaFimMnt)
                        { // Gerador está em manutenção na hora sorteada
                            gerador.estadoOperativo = 0;   // Definindo gerador como indisponível
                        }
                    }

                    // Sorteio para definir estado do gerador atual se não está em manutenção na hora sorteada
                    double x = rd.NextDouble();
                    if (x < gerador.FOR)
                    { // Gerador falhado para a hora sorteada
                        gerador.estadoOperativo = 0;       // Definindo gerador como indisponível
                    }

                    // Acumulando potência disponível
                    if (gerador.estadoOperativo == 1)
                    { // Gerador está disponível
                        potGerDisp += gerador.PotMax;
                    }
                }

                if (potGerDisp < cargaMaisReservaGer)
                { // Falha do sistema

                    // Atualizando índice LOLP para o período completo e semanal
                    AFlolp++;
                    A2Flolp++;
                    AFlolpPlanoSemanal[semanaSort - 1]++;

                    // Atualizando índice EPNS para o período completo e semanal
                    double defict = cargaMaisReservaGer - potGerDisp;                                      // Corte de carga
                    AFepns += defict;
                    A2Fepns += Math.Pow(defict, 2);
                    AFepnsPlanoSemanal[semanaSort - 1] += defict;

                    // Calculando índices
                    lolp = AFlolp / NS;
                    epns = AFepns / NS;

                    Vlolp = ((A2Flolp - NS * Math.Pow(lolp, 2)) / Math.Abs(NS * (NS - 1)));   // Variância LOLP
                    Vepns = ((A2Fepns - NS * Math.Pow(epns, 2)) / Math.Abs(NS * (NS - 1)));   // Variância EPNS
                    Blolp = (Math.Sqrt(Vlolp)) / (lolp);
                    Bepns = (Math.Sqrt(Vepns)) / (epns);

                    // Teste de convergência
                    if (Blolp < betaTol && Bepns < betaTol)
                    {
                        break;
                    }
                }

                // ------------------------------------------------
                // Calculando custo de produção
                double aGER = 0;                      // Geração acumulada
                foreach (Gerador gerador in sistema.GeradoresOrdCusto)
                {
                    if (gerador.estadoOperativo == 1)
                    { // Se gerador disponível para gerar                        
                        if ((aGER + gerador.PotMax) > cargaMaisReservaGer)
                        { // Último gerador a ser despachado (e não será despachado em sua capacidade máxima)
                            double complemento = cargaMaisReservaGer - aGER;
                            aGER += complemento;
                            custoMedioProd += complemento * gerador.custoGer;
                            gerador.eesSemanal[semanaSort - 1] += complemento;
                            gerador.receitaMedia += complemento * sistema.PLDMedioSemanal[semanaSort - 1];
                            break;
                        }
                        else
                        { // Despacho do próximo gerador mais barato
                            aGER = aGER + gerador.PotMax;
                            custoMedioProd += gerador.PotMax * gerador.custoGer;
                            gerador.eesSemanal[semanaSort - 1] += gerador.PotMax;
                            gerador.receitaMedia += gerador.PotMax * sistema.PLDMedioSemanal[semanaSort - 1];
                        }
                    }
                }
            }

            // Atualiza número estados necessários
            sistema.nEstadosConv += NS;
            NSAvaliacao = NS;

            // Calculando demais índices
            lole = sistema.nHorasPer * lolp;
            eens = sistema.nHorasPer * epns;
            this.custoMedioProd = this.custoMedioProd * sistema.nHorasPer / NS;
            this.receitaMediaProdutor = 0;
            foreach (Gerador gerador in sistema.GeradoresMnt)
            {
                this.receitaMediaProdutor += gerador.receitaMedia;
            }
            this.receitaMediaProdutor = this.receitaMediaProdutor * sistema.nHorasPer / NS;

            for (int i = 0; i < nSemPerido; i++)
            {
                lolpPlanoSemanal[i] = AFlolpPlanoSemanal[i] / NSsemanal[i];
                epnsPlanoSemanal[i] = AFepnsPlanoSemanal[i] / NSsemanal[i];
                lolePlanoSemanal[i] = 168 * lolpPlanoSemanal[i];
                eensPlanoSemanal[i] = 168 * epnsPlanoSemanal[i];
            }

            // Calculando EES de cada gerador
            foreach (Gerador gerador in sistema.Geradores)
            {
                for (int i = 0; i < nSemPerido; i++)
                {
                    // Dividindo valor acumulado de potência suprida dentro de cada semana pelo número de estados sorteados na semana
                    // Para obter energia, o valor obtido é multiplicado por 168 horas
                    gerador.eesSemanal[i] = 168 * (gerador.eesSemanal[i] / NSsemanal[i]);
                }
            }
        }
        # endregion

        // ------------------------------------------------
        // Método de execução da SMCNS com Entropia Cruzada (CE)
        # region Método de execução da rotina SMCNS com Entropia Cruzada (CE)
        public void ExecutaSMCNS_CE(int[] _vetorPlano)
        {
            zeraVariaveis();
            
            rd = new Random(sementeSMCNS);         // Instanciando objeto para obtenção de número aleatório
            foreach (Gerador gerador in sistema.Geradores)
            {
                gerador.eesSemanal = new double[nSemPerido];     // Instanciando o vetor que contém valores semanais de EES do gerador
            }

            // Atualiza número de avaliações de confiabilidade
            sistema.contAvalConf++;

            // ------------------------------------------------
            // Iterações da SMCNS
            while (NS < NSMAX)
            {
                potGerDisp = 0;       // Definindo potência geração disponível igual a zero para iniciar a avaliação do estado

                // ------------------------------------------------
                // Sorteio de hora dentro do período de estudo
                double y = rd.NextDouble();                                                // Sorteio de número pseudoaleatório para definir nível de carga
                int posHora = Convert.ToInt16((sistema.curvaCarga.Length - 1) * y);        // Posição da hora sorteada dentro da curva de carga
                int horaSort = posHora + 1;                                                // Hora sorteada dentro do período de estudo
                double carga = sistema.curvaCarga[posHora] * sistema.cargaTotal;           // Valor, em MW, da carga para a hora sorteada
                double cargaMaisReservaGer = sistema.curvaCarga[posHora] * (sistema.cargaTotal + sistema.testeAtual.reservaGer);     // Carga mais a reserva de geração a ser atendida
                double semanaSortDouble = (horaSort / 168.0) + 0.99999999;                 // Semana sorteada dentro do período de estudo
                int semanaSort = Convert.ToInt16(Math.Floor(semanaSortDouble));

                // ------------------------------------------------
                // Atualização da contagem de estados sorteados
                NS++;                          // Número total de estados sorteados            
                NSsemanal[semanaSort - 1]++;   // Número de estados sorteados dentro de cada semana

                // ------------------------------------------------
                // Verificação do estado operativo dos geradores
                foreach (Gerador gerador in sistema.Geradores)
                {
                    // Definindo gerador como disponível (para iniciair a avaliação)
                    gerador.estadoOperativo = 1;
                    gerador.estadoOperativoFalha = 1;

                    // Definindo estado operativo do gerador de acordo com plano de manutenção para a hora sorteada
                    if (gerador.progMnt == true)
                    {
                        if (horaSort >= gerador.horaIniMnt && horaSort <= gerador.horaFimMnt)
                        { // Gerador está em manutenção na hora sorteada
                            gerador.estadoOperativo = 0;   // Definindo gerador como indisponível
                        }
                    }

                    // Definindo estado operativo do gerador de acordo com informação de programação previamente definido (se gerador já está previamente programado)
                    if (gerador.progMnt == false && gerador.gerJaProgMnt == true)
                    {
                        if (horaSort >= gerador.horaIniMnt && horaSort <= gerador.horaFimMnt)
                        { // Gerador está em manutenção na hora sorteada
                            gerador.estadoOperativo = 0;   // Definindo gerador como indisponível
                        }
                    }

                    // Sorteio para definir estado do gerador atual se não está em manutenção na hora sorteada
                    double x = rd.NextDouble();
                    if (x < gerador.FOR_CE)
                    { // Gerador falhado para a hora sorteada
                        gerador.estadoOperativo = 0;       // Definindo gerador como indisponível
                        gerador.estadoOperativoFalha = 0;  // Definindo gerador como indisponível para cálculo de verosimilhança
                    }

                    // Acumulando potência disponível
                    if (gerador.estadoOperativo == 1)
                    { // Gerador está disponível
                        potGerDisp += gerador.PotMax;
                    }
                }

                // ------------------------------------------------
                // Calculando valores de verosimilhança W
                // Calculado para todos os estados devido ao cálculo do custo médio de produção
                double numProd = 1;
                double denProd = 1;
                for (int j = 0; j < sistema.Geradores.Count(); j++)
                {
                    double uGj = sistema.Geradores[j].FOR;
                    double vGj = sistema.Geradores[j].FOR_CE;
                    int xj = sistema.Geradores[j].estadoOperativoFalha;
                    numProd *= (Math.Pow((1 - uGj), xj) * Math.Pow((uGj), (1 - xj)));
                    denProd *= (Math.Pow((1 - vGj), xj) * Math.Pow((vGj), (1 - xj)));
                }
                double W = numProd / denProd;

                // ------------------------------------------------
                // Verificando estado do Sistema (sucesso ou falha)
                if (potGerDisp < cargaMaisReservaGer)
                { // Falha do sistema

                    // Atualizando índice LOLP para o período completo e semanal
                    AFlolp += W;
                    A2Flolp += Math.Pow(W, 2);
                    AFlolpPlanoSemanal[semanaSort - 1] += W;

                    // Atualizando índice EPNS para o período completo e semanal
                    double defict = cargaMaisReservaGer - potGerDisp;                                      // Corte de carga
                    AFepns += defict * W;
                    A2Fepns += Math.Pow((defict * W), 2);
                    AFepnsPlanoSemanal[semanaSort - 1] += defict * W;

                    // Calculando índices
                    lolp = AFlolp / NS;
                    epns = AFepns / NS;

                    Vlolp = ((A2Flolp - NS * Math.Pow(lolp, 2)) / Math.Abs(NS * (NS - 1)));   // Variância LOLP
                    Vepns = ((A2Fepns - NS * Math.Pow(epns, 2)) / Math.Abs(NS * (NS - 1)));   // Variância EPNS
                    Blolp = (Math.Sqrt(Vlolp)) / (lolp);
                    Bepns = (Math.Sqrt(Vepns)) / (epns);

                    // Teste de convergência
                    if (Blolp < betaTol && Bepns < betaTol)
                    {
                        break;
                    }
                }

                // ------------------------------------------------
                // Calculando custo de produção
                double aGER = 0;                      // Geração acumulada
                double auxCustoMedioProd = 0;
                foreach (Gerador gerador in sistema.GeradoresOrdCusto)
                {
                    if (gerador.estadoOperativo == 1)
                    { // Se gerador disponível para gerar      

                        if ((aGER + gerador.PotMax) > cargaMaisReservaGer)
                        { // Último gerador a ser despachado (e não será despachado em sua capacidade máxima)
                            double complemento = cargaMaisReservaGer - aGER;
                            aGER += complemento;
                            // custoMedioProd += complemento * gerador.custoGer;
                            auxCustoMedioProd += complemento * gerador.custoGer;
                            gerador.eesSemanal[semanaSort - 1] += complemento * W;
                            break;
                        }
                        else
                        { // Despacho do próximo gerador mais barato
                            aGER = aGER + gerador.PotMax;
                            // custoMedioProd += gerador.PotMax * gerador.custoGer;
                            auxCustoMedioProd += gerador.PotMax * gerador.custoGer;
                            gerador.eesSemanal[semanaSort - 1] += gerador.PotMax * W;
                        }
                    }
                }
                custoMedioProd += auxCustoMedioProd * W;
            }

            // Atualiza número estados necessários
            sistema.nEstadosConv += NS;
            NSAvaliacao = NS;

            // Calculando demais índices
            lole = sistema.nHorasPer * lolp;
            eens = sistema.nHorasPer * epns;
            this.custoMedioProd = this.custoMedioProd * sistema.nHorasPer / NS;

            for (int i = 0; i < nSemPerido; i++)
            {
                lolpPlanoSemanal[i] = AFlolpPlanoSemanal[i] / NSsemanal[i];
                epnsPlanoSemanal[i] = AFepnsPlanoSemanal[i] / NSsemanal[i];
                lolePlanoSemanal[i] = 168 * lolpPlanoSemanal[i];
                eensPlanoSemanal[i] = 168 * epnsPlanoSemanal[i];
            }

            // Calculando EES de cada gerador
            foreach (Gerador gerador in sistema.Geradores)
            {
                for (int i = 0; i < nSemPerido; i++)
                {
                    // Dividindo valor acumulado de potência suprida dentro de cada semana pelo número de estados sorteados na semana
                    // Para obter energia, o valor obtido é multiplicado por 168 horas
                    gerador.eesSemanal[i] = 168 * (gerador.eesSemanal[i] / NSsemanal[i]);
                }
            }
        }
        #endregion

        // ------------------------------------------------
        // Método para determinar distorção dos parâmetros FOR com base na Entropia Cruzada (CE)
        #region Método para distorção dos parâmetros FOR via Entropia Cruzada (CE)
        public void DistorcaoParametrosCE()
        {
            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
            // Definição provisória a partir de resultados do Matlab
            double[] novosFORs = { 0.0279, 0.1607, 0.1394, 0.0321, 0.0481, 0.0643, 0.0430, 0.1069, 0.0909, 0.1673, 0.3627, 0.4909, 0.4909, 0.0125 };
            int[] nGUsinas = { 5, 2, 2, 2, 2, 3, 1, 1, 2, 3, 1, 1, 1, 6 };
            int contGer = 0;
            for (int k = 0; k < nGUsinas.Length; k++)
            {
                for(int i = 0; i < nGUsinas[k];i++)
                {
                    sistema.Geradores[contGer].FOR_CE = novosFORs[k];
                    contGer++;
                }
            }
            // &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
        }
        #endregion
    }
    # endregion
}
