<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <xsl:output method="html"/>

  <xsl:template match="/">    
    <!--Courier, Tahoma-->
    <!--<div style="font-family: Tahoma; font-size: 10pt; margin-bottom: 2em;">-->
      <xsl:apply-templates />
    <!--</div>-->
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