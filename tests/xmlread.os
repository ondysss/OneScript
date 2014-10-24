﻿///////////////////////////////////////////////////////////////////////
//
// Приемочные тесты объекта ЧтениеXML
// 
//
///////////////////////////////////////////////////////////////////////

Перем юТест;

////////////////////////////////////////////////////////////////////
// Программный интерфейс

Функция Версия() Экспорт
	Возврат "0.1";
КонецФункции

Функция ПолучитьСписокТестов(ЮнитТестирование) Экспорт
	
	юТест = ЮнитТестирование;
	
	ВсеТесты = Новый Массив;
	
	ВсеТесты.Добавить("ТестДолжен_ПроверитьВерсию");
	ВсеТесты.Добавить("ТестДолжен_ПрочитатьЭлементыИзСтроки");
	
	Возврат ВсеТесты;
КонецФункции

Процедура ТестДолжен_ПроверитьВерсию() Экспорт
	Сообщить("Версия() = "+Версия());
КонецПроцедуры

Процедура ТестДолжен_ПрочитатьЭлементыИзСтроки() Экспорт
	
	ЧтениеXML = Новый ЧтениеXML;
	ЧтениеXML.УстановитьСтроку(СтрокаXML());
	
	юТест.ПроверитьИстину(ЧтениеXML.Прочитать(),"Данные XML пусты");
	юТест.ПроверитьРавенство(ЧтениеXML.Имя, "xml");
	юТест.ПроверитьРавенство(ЧтениеXML.ТипУзла, ТипУзлаXML.НачалоЭлемента);
	
	ЧтениеXML.Прочитать();
	юТест.ПроверитьРавенство(ЧтениеXML.ТипУзла, ТипУзлаXML.НачалоЭлемента, "тест типа узла: НачалоЭлемента data");
	ЧтениеXML.Прочитать();
	юТест.ПроверитьРавенство(ЧтениеXML.Значение, "hello");
	ЧтениеXML.Прочитать();
	юТест.ПроверитьРавенство(ЧтениеXML.ТипУзла, ТипУзлаXML.КонецЭлемента, "тест типа узла: КонецЭлемента data");
	ЧтениеXML.Прочитать();
	юТест.ПроверитьРавенство(ЧтениеXML.ТипУзла, ТипУзлаXML.КонецЭлемента, "тест типа узла: Текст КонецЭлемента xml");
	
	ЧтениеXML.Закрыть();
	
КонецПроцедуры

Функция СтрокаXML()

	Текст = 
	"<xml>
	|	<data>hello</data>
	|</xml>";
	
	Возврат Текст;

КонецФункции