[![.NET](https://github.com/zamgi/lingvo--NER--German/actions/workflows/dotnet.yml/badge.svg)](https://github.com/zamgi/lingvo--NER--German/actions/workflows/dotnet.yml)

# NER
Named-entity recognition in German language using combined of deep neural network and ruled-based approach in C# for .NET

#
Metrics for includes models:

 ner_de__em128__e6xm8_[union]:
```
Common-Score: '95.42'

B-PER: F-score = '97.98' Precision = '98.34' Recall = '97.62'
I-PER: F-score = '98.39' Precision = '98.70' Recall = '98.09'
B-LOC: F-score = '95.70' Precision = '95.81' Recall = '95.58'
I-LOC: F-score = '94.55' Precision = '93.61' Recall = '95.50'
B-ORG: F-score = '92.62' Precision = '92.84' Recall = '92.41'
I-ORG: F-score = '93.29' Precision = '93.73' Recall = '92.86'

The number of categories = '6' of '6'
```
 ner_de__em128__e6xm8_[union]_(upper):
```
Common-Score: '95.47'

B-PER: F-score = '98.04' Precision = '98.54' Recall = '97.54'
I-PER: F-score = '98.34' Precision = '98.74' Recall = '97.95'
B-LOC: F-score = '95.58' Precision = '95.79' Recall = '95.38'
I-LOC: F-score = '94.99' Precision = '94.96' Recall = '95.01'
B-ORG: F-score = '92.35' Precision = '92.62' Recall = '92.08'
I-ORG: F-score = '93.55' Precision = '94.46' Recall = '92.64'

The number of categories = '6' of '6'
```
#

Included NER UI sample:
![alt tag](https://github.com/zamgi/lingvo--NER--German/blob/master/ner.combined.german.png)
