<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html"/>

  <xsl:template match="/">    
    <!--
    <html>
      <head>
        <title>xml-as-html</title>
      </head>
      <body>
    -->
        <!--Courier, Tahoma-->
        <div style="font-family: Tahoma; font-size: 10pt; margin-bottom: 2em;">
          <xsl:apply-templates />
        </div>
    <!--   
      </body>
    </html>
    -->
  </xsl:template>

  <xsl:template match="*">
    <div style="margin-left: 1em; color: maroon;">
      &lt;<xsl:value-of select="name()"/><xsl:apply-templates select="@*"/>/&gt;
    </div>
  </xsl:template>

  <xsl:template match="*[node()]">
    <div style="margin-left: 1em;">
      <span style="color: maroon;">
        &lt;<xsl:value-of select="name()"/><xsl:apply-templates select="@*"/>&gt;</span>
      <xsl:apply-templates select="node()"/>
      <span style="color: maroon;">&lt;/<xsl:value-of select="name()"/>&gt;</span>
    </div>
  </xsl:template>

  <xsl:template match="text()">
    <xsl:choose>
      <xsl:when test="normalize-space() != ''">
        <span style="color: black;"><xsl:value-of select="." /></span>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="." />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!--
  <xsl:template match="cdata()">
    <pre>
      &lt;![CDATA[<xsl:value-of select="." />]]&gt;
    </pre>
  </xsl:template>
  -->

  <xsl:template match="comment()">
    <div style="margin-left: 1em; color: green;">
      &lt;!-- <xsl:value-of select="." /> --&gt;
    </div>
  </xsl:template>

  <xsl:template match="@*">
    <span style="color: blue;">
      <xsl:text> </xsl:text>
      <xsl:value-of select="name()"/>="<span style="color: black;"><xsl:value-of select="." /></span>"</span>
  </xsl:template>

</xsl:stylesheet>