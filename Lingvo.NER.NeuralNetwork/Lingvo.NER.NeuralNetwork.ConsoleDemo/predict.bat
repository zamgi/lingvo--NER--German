rem .\bin\Lingvo.NER.NeuralNetwork.ConsoleDemo.exe -Task Predict -ModelFilePath ..\[resources]\models\ner_de.s2s -InputTestFile ..\[resources]\input-text\input_ner_de.txt -OutputFile output_ner_de.txt -MaxPredictSentLength 110 -ProcessorType CPU
bin\Lingvo.NER.NeuralNetwork.ConsoleDemo.exe -ConfigFilePath "predict.json"

pause
