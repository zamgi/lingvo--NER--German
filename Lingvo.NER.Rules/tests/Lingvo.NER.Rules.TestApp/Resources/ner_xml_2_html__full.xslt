<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html"/>

  <xsl:template match="/">    
    <html>
      <head>
        <title>NER-markup</title>
        <style type="text/css" id="NER__css">
        .NER {
            border-bottom: 1px dotted black;
            font-weight: bold;
        }
        .PhoneNumber, .phoneNumber {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: #c1c100; /*darkviolet;*/ /*#222;*/
        }
        .Url, .Email, .url, .email {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: lightseagreen;
        }
        .CustomerNumber, .customerNumber {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: darkorange;
        }
        .Birthday, .birthday {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: darkorchid;
        }
        .Birthplace, .birthplace {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: orchid;
        }
        .MaritalStatus, .maritalStatus {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: darkred;
        }
        .Nationality, .nationality {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: darkblue;
        }
        .CreditCard, .creditCard {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: cyan;
        }
        .PassportIdCardNumber, .passportIdCardNumber {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: mediumvioletred;
        }
        .CarNumber, .carNumber {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: crimson;
        }
        .HealthInsurance, .healthInsurance {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: seagreen;
        }
        .DriverLicense, .driverLicense {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: cadetblue;
        }
        .SocialSecurity, .socialSecurity {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: darkorchid;
        }
        .TaxIdentification, .taxIdentification {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: magenta;
        }
        .Address, .address {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: orangered;
        }
        .AccountNumber, .accountNumber {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: blueviolet;
        }
        .Name, .name {
            border-bottom: 1px dotted black;
            font-weight: bold;
            color: rgb(167, 40, 40);
        }
        </style>
      </head>
      <body>
    
        <!--Courier, Tahoma-->
        <div style="font-family: Tahoma; font-size: 10pt; margin-bottom: 2em;">
          <xsl:apply-templates />
        </div>
    
      </body>
    </html>
  </xsl:template>

  <xsl:template match="*[node()]">
    <!--<xsl:choose>
      <xsl:when test="@*">-->
        <span class="{name()}">
          
          <xsl:choose>
            <xsl:when test="@*">
              <xsl:for-each select="@*">
                <xsl:attribute name="{name()}"><xsl:value-of select="." /></xsl:attribute>        
              </xsl:for-each>
          
              <xsl:attribute name="title">
                <xsl:text>[</xsl:text><xsl:value-of select="name()"/><xsl:text>]</xsl:text>
                <xsl:text>, &#xA;</xsl:text>
                <xsl:for-each select="@*">
                  <xsl:value-of select="name()" /><xsl:text>: </xsl:text><xsl:value-of select="." />       
                  <xsl:if test="position() != last()">
                    <xsl:text>, &#xA;</xsl:text>
                  </xsl:if>
                </xsl:for-each>
              </xsl:attribute>
            </xsl:when>
            <xsl:otherwise>
              <xsl:attribute name="title">
                <xsl:text>[</xsl:text><xsl:value-of select="name()"/><xsl:text>]</xsl:text>
              </xsl:attribute>                
            </xsl:otherwise>
          </xsl:choose>
      
          <xsl:apply-templates select="node()"/>
        </span>
      <!--</xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="node()"/>  
      </xsl:otherwise>
    </xsl:choose>-->    
  </xsl:template>

  <xsl:template match="span[@class]">
    <xsl:copy-of select="." />
  </xsl:template>

  <xsl:template match="text()">
    <xsl:value-of select="." />
  </xsl:template>

</xsl:stylesheet>